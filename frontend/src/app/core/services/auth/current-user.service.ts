import { Injectable, inject, computed } from '@angular/core';
import { AuthFacadeService } from './auth-facade.service';

@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private auth = inject(AuthFacadeService);

  currentUser = computed(() => this.auth.currentUser());
  isAuthenticated = computed(() => this.auth.isAuthenticated());
  isAdmin = computed(() => this.auth.isAdmin());
  isDoctor = computed(() => this.auth.isDoctor());
  isPatient = computed(() => this.auth.isPatient());
  isPharmacist = computed(() => this.auth.isPharmacist());
  isPharmacyStaff = computed(() => this.auth.isPharmacyStaff());
  isManager = computed(() => this.auth.isManager());
  isEmployee = computed(() => this.auth.isEmployee());

  get snapshot() {
    return this.auth.currentUser();
  }

  getDefaultRoute(): string {
    const user = this.snapshot;
    if (!user) return '/auth/login';

    switch (user.userType) {
      case 'Admin':
        return '/admin';
      case 'Doctor':
        return '/doctor';
      case 'Patient':
        return '/patient';
      case 'Pharmacist':
        return '/pharmacy/dashboard';
      default:
        return '/';
    }
  }
}
