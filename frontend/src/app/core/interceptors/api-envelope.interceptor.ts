import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { catchError, mergeMap, of, throwError } from 'rxjs';
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

export const apiEnvelopeInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.includes('/hubs') || req.url.includes('/health')) {
    return next(req);
  }

  return next(req).pipe(
    mergeMap((event) => {
      if (!(event instanceof HttpResponse)) {
        return of(event);
      }
      if (event.status === 204 || event.body === null) {
        return of(event);
      }
      const body = event.body;
      if (isApiEnvelope(body)) {
        if (!body.success) {
          return throwError(
            () =>
              new HttpErrorResponse({
                error: { message: body.message ?? 'Request failed', traceId: body.traceId },
                headers: event.headers,
                status: 400,
                statusText: 'Bad Request',
                url: event.url ?? undefined,
              }),
          );
        }
        return of(event.clone({ body: body.data }));
      }
      return of(event);
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
