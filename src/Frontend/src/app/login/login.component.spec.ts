import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideHttpClient(), provideRouter([])]
    }).compileComponents();
  });

  it('requires email and password', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.submit();

    expect(component.form.invalid).toBeTrue();
    expect(component.form.controls.email.hasError('required')).toBeTrue();
    expect(component.form.controls.password.hasError('required')).toBeTrue();
  });

  it('validates the email format', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    const component = fixture.componentInstance;

    component.form.controls.email.setValue('not-an-email');

    expect(component.form.controls.email.hasError('email')).toBeTrue();
  });
});
