
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import { firstValueFrom } from 'rxjs';
import { filter } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';
import { CdeService } from '../services/cde';

@Component({
  selector: 'app-cde-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './cde-shell.html',
  styleUrl: './cde-shell.scss'
})
export class CdeShell implements OnInit {

  userName = 'CDE';
  role = 'CDE';
  county = '';
  unread = 0;

  // Sidebar collapse state
  sidebarCollapsed = false;

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }

  // Education levels available in the top-bar dropdown.
  educationLevels: string[] = [
    'Junior School',
    'Primary School',
    'Senior School'
  ];

  selectedLevel = 'Junior School';

  constructor(
    private authService: AuthService,
    private cdeService: CdeService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (typeof window === 'undefined') {
      return;
    }

    this.loadUserDetails();
    this.refreshUnread();

    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.refreshUnread();
      });
  }

  private loadUserDetails(): void {
    try {
      const user = JSON.parse(
        localStorage.getItem('kemis_user') ?? 'null'
      );

      if (user) {
        this.userName = user.fullName || 'CDE';
        this.role = user.role || 'CDE';
        this.county = user.county || '';
      }
    } catch {
      // Keep default user details if stored data cannot be read.
    }
  }

  onLevelChange(event: Event): void {
    const select = event.target as HTMLSelectElement;

    this.selectedLevel = select.value;

    this.cdr.detectChanges();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private async refreshUnread(): Promise<void> {
    try {
      const notifications = await firstValueFrom(
        this.cdeService.getNotifications()
      );

      this.unread = notifications.filter(
        notification => !notification.isRead
      ).length;

    } catch {
      this.unread = 0;
    }

    this.cdr.detectChanges();
  }
}
