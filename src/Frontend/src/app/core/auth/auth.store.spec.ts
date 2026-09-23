import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth.store';

describe('AuthStore', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [AuthStore] });
  });

  it('hydrates the token from localStorage and persists changes', () => {
    localStorage.setItem('identityhub_token', 'persisted-token');
    const store = TestBed.inject(AuthStore);

    expect(store.token()).toBe('persisted-token');
    expect(store.isAuthenticated()).toBeTrue();

    store.setToken('new-token');
    expect(localStorage.getItem('identityhub_token')).toBe('new-token');

    store.clearToken();
    expect(store.token()).toBeNull();
    expect(store.isAuthenticated()).toBeFalse();
  });

  it('tracks loading and error state with signals', () => {
    const store = TestBed.inject(AuthStore);

    store.setLoading(true);
    store.setError('Login failed');

    expect(store.loading()).toBeTrue();
    expect(store.error()).toBe('Login failed');
  });
});
