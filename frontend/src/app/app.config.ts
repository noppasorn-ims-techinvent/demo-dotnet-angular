import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import type { AppConfigDto } from './core/models/api.types';
import { AppConfigService } from './core/services/app-config.service';
import { AuthService } from './core/services/auth.service';
import { MarketplaceHubService } from './core/services/marketplace-hub.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor])),
    provideRouter(routes),
    provideAppInitializer(() => {
      const auth = inject(AuthService);
      const hub = inject(MarketplaceHubService);
      const http = inject(HttpClient);
      const appCfg = inject(AppConfigService);

      return firstValueFrom(http.get<AppConfigDto>('/api/App/config'))
        .then((dto) => {
          appCfg.applyFromApi(dto);
        })
        .catch(() => undefined)
        .finally(async () => {
          auth.hydrateFromStorage();
          const user = auth.user();
          const token = auth.token();
          const roles = user?.roles ?? [];
          if (user && token && (roles.includes('Buyer') || roles.includes('Seller') || roles.includes('Admin'))) {
            try {
              await hub.connectForUser(user, token);
            } catch {
              /* hub ล้ม — แอปยังใช้งานได้ */
            }
          }
        });
    }),
  ],
};
