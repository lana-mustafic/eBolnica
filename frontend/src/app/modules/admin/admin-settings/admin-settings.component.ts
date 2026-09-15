import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { AdminApiService } from '../../../api-services/admin/admin-api.service';
import { AdminProfileDto } from '../../../api-services/admin/admin-api.models';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { BaseComponent } from '../../../core/components/base-classes/base-component';

@Component({
  selector: 'app-admin-settings',
  standalone: false,
  templateUrl: './admin-settings.component.html',
  styleUrl: './admin-settings.component.scss',
})
export class AdminSettingsComponent extends BaseComponent implements OnInit {
  private adminApi = inject(AdminApiService);
  private fb = inject(FormBuilder);
  private toaster = inject(ToasterService);
  private translate = inject(TranslateService);
  auth = inject(AuthFacadeService);

  profile: AdminProfileDto | null = null;
  currentLang = this.translate.currentLang || localStorage.getItem('language') || 'bs';

  languages = [
    { code: 'bs', name: 'Bosanski', flag: '🇧🇦' },
    { code: 'en', name: 'English', flag: '🇬🇧' },
  ];

  form = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.startLoading();
    this.adminApi.getMyProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.form.patchValue({
          firstName: profile.firstName?.trim() || 'Admin',
          lastName: profile.lastName?.trim() || 'Korisnik',
          email: profile.email,
        });
        this.stopLoading();
      },
      error: (err) => this.stopLoading(err?.error?.message ?? 'Greška pri učitavanju profila.'),
    });
  }

  saveProfile(): void {
    if (!this.profile || this.form.invalid || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    this.startLoading();
    const raw = this.form.getRawValue();
    this.adminApi
      .updateUser(this.profile.appUserId, {
        firstName: raw.firstName ?? '',
        lastName: raw.lastName ?? '',
        email: raw.email ?? '',
      })
      .subscribe({
        next: () => {
          this.toaster.success('Profil je sačuvan.');
          this.loadProfile();
        },
        error: (err) => this.stopLoading(err?.error?.message ?? 'Spremanje profila nije uspjelo.'),
      });
  }

  switchLanguage(langCode: string): void {
    this.currentLang = langCode;
    this.translate.use(langCode);
    localStorage.setItem('language', langCode);
    this.toaster.success(langCode === 'bs' ? 'Jezik postavljen na bosanski.' : 'Language set to English.');
  }

  roleLabel(profile: AdminProfileDto): string {
    if (profile.isAdmin) {
      return 'Administrator';
    }
    return profile.userType || 'Korisnik';
  }
}
