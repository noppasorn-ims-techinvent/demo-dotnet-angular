import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { catchError, map, throwError } from 'rxjs';

/** รูปแบบเดียวกับ backend.Models.Api.ApiResponse */
interface ApiEnvelope {
  success: boolean;
  message?: string;
  traceId?: string;
  data: unknown;
}

function isApiEnvelope(body: unknown): body is ApiEnvelope {
  return (
    body !== null &&
    typeof body === 'object' &&
    'success' in body &&
    'data' in body &&
    typeof (body as ApiEnvelope).success === 'boolean'
  );
}

/** ถอด envelope { success, message, traceId, data } → ให้ HttpClient เห็นแค่ data (สำเร็จ) หรือ error.message (ล้มเหลว) */
export const apiEnvelopeInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.includes('/hubs') || req.url.includes('/health')) {
    return next(req);
  }

  return next(req).pipe(
    map((event) => {
      if (!(event instanceof HttpResponse)) {
        return event;
      }
      if (event.status === 204 || event.body === null) {
        return event;
      }
      const body = event.body;
      if (isApiEnvelope(body)) {
        if (!body.success) {
          return event;
        }
        return event.clone({ body: body.data });
      }
      return event;
    }),
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        const b = err.error;
        if (isApiEnvelope(b) && !b.success) {
          return throwError(
            () =>
              new HttpErrorResponse({
                error: { message: b.message ?? 'Request failed', traceId: b.traceId },
                headers: err.headers,
                status: err.status,
                statusText: err.statusText,
                url: err.url ?? undefined,
              }),
          );
        }
      }
      return throwError(() => err);
    }),
  );
};
