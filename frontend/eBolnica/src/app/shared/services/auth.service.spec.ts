import { TestBed } from '@angular/core/testing';

import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getUserId returns JWT sub claim', () => {
    const token = createToken({ sub: 'user-123', role: 'Pharmacist' });
    spyOn(service, 'getToken').and.returnValue(token);

    expect(service.getUserId()).toBe('user-123');
  });

  it('getUserId returns null when token is missing or invalid', () => {
    spyOn(service, 'getToken').and.returnValue(null);
    expect(service.getUserId()).toBeNull();

    spyOn(service, 'getToken').and.returnValue('not-a-jwt');
    expect(service.getUserId()).toBeNull();
  });
});

function createToken(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.signature`;
}
