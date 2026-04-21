import { HttpEvent, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { map } from 'rxjs/operators';

/** ตรงกับ backend.Models.Responses.ApiResponse (camelCase) */
function isApiEnvelope(body: unknown): body is { success: boolean; message: string; traceId: string; data: unknown } {
  if (body === null || typeof body !== 'object') {
    return false;
  }
  const b = body as Record<string, unknown>;
  return (
    typeof b['success'] === 'boolean' &&
    typeof b['message'] === 'string' &&
    typeof b['traceId'] === 'string' &&
    'data' in b
  );
}

/** ถอด { success, message, traceId, data } → ใช้แค่ data ให้ HttpClient เหมือนเดิม */
export const apiEnvelopeInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/')) {
    return next(req);
  }

  return next(req).pipe(
    map((event: HttpEvent<unknown>) => {
      if (event instanceof HttpResponse && event.body !== null && event.body !== undefined && isApiEnvelope(event.body)) {
        return event.clone({ body: event.body.data });
      }
      return event;
    }),
  );
};
