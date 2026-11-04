import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { toApiError } from './problem-details';

export const problemDetailsInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(catchError((error: unknown) => throwError(() => toApiErrorIfHttp(error))));

function toApiErrorIfHttp(error: unknown): unknown {
  if (error instanceof HttpErrorResponse) {
    return toApiError(error);
  }
  return error;
}
