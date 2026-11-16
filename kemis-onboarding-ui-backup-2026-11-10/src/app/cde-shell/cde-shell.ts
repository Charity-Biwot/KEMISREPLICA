import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { filter } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';
import { CdeService } from '../services/cde';

@Component({
  selector: 'app-cde-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './cde-shell.html',
  styleUrl: './cde-shell.scss'
})
export class CdeShell implements OnInit {
  userName = 'CDE';
  role = 'CDE';
  county = '';
  unread = 0;

  // Menu items on the real KEMIS screen that are not built here yet.
  soon = ['Menu Search', 'KEMIS Reports', 'Escalations', 'Registration', 'User Manual'];

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

    try {
      const user = JSON.parse(localStorage.getItem('kemis_user') ?? 'null');
      if (user) {
        this.userName = user.fullName || 'CDE';
        this.role = user.role || 'CDE';
        this.county = user.county || '';
      }
    } catch {
      // Keep the defaults.
    }

    this.refreshUnread();
    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.refreshUnread());
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private async refreshUnread(): Promise<void> {
    try {
      const list = await firstValueFrom(this.cdeService.getNotifications());
      this.unread = list.filter(n => !n.isRead).length;
    } catch {
      this.unread = 0;
    }
    this.cdr.detectChanges();
  }
}