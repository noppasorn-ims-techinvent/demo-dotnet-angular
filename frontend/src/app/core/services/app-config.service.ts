import { Injectable, signal } from '@angular/core';
import type { AppConfigDto } from '../models/api.types';

@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly _config = signal<AppConfigDto | null>(null);

  readonly config = this._config.asReadonly();

  applyFromApi(dto: AppConfigDto): void {
    this._config.set(dto);
  }

  appTitle(): string {
    return this._config()?.appName ?? 'ร้านค้า';
  }
}
