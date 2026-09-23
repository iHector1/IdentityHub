import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { jwtInterceptor } from './jwt.interceptor';

describe('jwtInterceptor', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.setItem('identityhub_token', 'test-token');
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([jwtInterceptor])),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    localStorage.clear();
    http.verify();
  });

  it('adds the bearer token to requests', () => {
    TestBed.inject(HttpClient).get('/api/users').subscribe();

    const request = http.expectOne('/api/users');
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush([]);
  });
});
