import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, tap, throwError } from 'rxjs';
import type { AuthResponse, UserDto } from '../models/api.types';
import { MarketplaceHubService } from './marketplace-hub.service';

const TOKEN_KEY = 'access_token';
const USER_KEY = 'user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly hub = inject(MarketplaceHubService);

  private readonly _token = signal<string | null>(null);
  private readonly _user = signal<UserDto | null>(null);
  private readonly _busy = signal(false);

  readonly token = this._token.asReadonly();
  readonly user = this._user.asReadonly();
  readonly busy = this._busy.asReadonly();

  readonly isAuthenticated = computed(() => !!this._token());

  hydrateFromStorage(): void {
    const token = localStorage.getItem(TOKEN_KEY);
    const raw = localStorage.getItem(USER_KEY);
    if (token) {
      this._token.set(token);
    }
    if (raw) {
      try {
        this._user.set(JSON.parse(raw) as UserDto);
      } catch {
        localStorage.removeItem(USER_KEY);
      }
    }
  }

  hasRole(role: string): boolean {
    return this._user()?.roles?.includes(role) ?? false;
  }

  login(email: string, password: string) {
    this._busy.set(true);
    return this.http.post<AuthResponse>('/api/Auth/LoginAsync', { email, password }).pipe(
      tap((res) => this.persistSession(res)),
      tap((res) => this.connectRealtimeIfNeeded(res.user)),
      finalize(() => this._busy.set(false)),
      catchError((err) => throwError(() => err)),
    );
  }

  register(email: string, password: string, displayName: string) {
    this._busy.set(true);
    return this.http.post<AuthResponse>('/api/Auth/RegisterAsync', { email, password, displayName }).pipe(
      tap((res) => this.persistSession(res)),
      tap((res) => this.connectRealtimeIfNeeded(res.user)),
      finalize(() => this._busy.set(false)),
      catchError((err) => throwError(() => err)),
    );
  }

  logout(): void {
    void this.hub.disconnect();
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._user.set(null);
    void this.router.navigateByUrl('/login');
  }

  private persistSession(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this._token.set(res.token);
    this._user.set(res.user);
  }

  private connectRealtimeIfNeeded(user: UserDto): void {
    const r = user.roles ?? [];
    if (r.includes('Buyer') || r.includes('Seller') || r.includes('Admin')) {
      void this.hub.connectForUser({ ...user, roles: r }, this._token() ?? '');
    }
  }
}
