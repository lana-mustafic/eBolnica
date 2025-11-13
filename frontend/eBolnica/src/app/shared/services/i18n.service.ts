import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, forkJoin, combineLatest } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';

export type Language = 'en' | 'bs';

export interface Translations {
  [key: string]: string | Translations;
}

@Injectable({
  providedIn: 'root'
})
export class I18nService {
  private http: HttpClient | null = null;
  private currentLanguage$ = new BehaviorSubject<Language>('en');
  private translations: { [lang: string]: Translations } = {};
  private translationsLoaded$ = new BehaviorSubject<boolean>(false);

  constructor() {
    // Load saved language from localStorage or default to 'en'
    const savedLang = localStorage.getItem('app-language') as Language;
    if (savedLang && (savedLang === 'en' || savedLang === 'bs')) {
      this.currentLanguage$.next(savedLang);
    }
  }

  initialize(http: HttpClient): void {
    this.http = http;
    this.loadAllTranslations();
  }

  private loadAllTranslations(): void {
    if (!this.http) {
      console.error('HttpClient not initialized');
      return;
    }
    
    forkJoin({
      en: this.http.get<Translations>(`assets/i18n/en.json`),
      bs: this.http.get<Translations>(`assets/i18n/bs.json`)
    }).subscribe({
      next: (translations) => {
        this.translations['en'] = translations.en;
        this.translations['bs'] = translations.bs;
        this.translationsLoaded$.next(true);
      },
      error: (err) => {
        console.error('Failed to load translations:', err);
        this.translationsLoaded$.next(true);
      }
    });
  }

  getTranslationsLoaded(): Observable<boolean> {
    return this.translationsLoaded$.asObservable();
  }

  isTranslationsLoaded(): boolean {
    return this.translationsLoaded$.value;
  }

  setLanguage(lang: Language): void {
    this.currentLanguage$.next(lang);
    localStorage.setItem('app-language', lang);
  }

  getCurrentLanguage(): Observable<Language> {
    return this.currentLanguage$.asObservable();
  }

  getCurrentLanguageValue(): Language {
    return this.currentLanguage$.value;
  }

  translate(key: string, params?: { [key: string]: string }): string {
    const lang = this.currentLanguage$.value;
    
    // Try to get translation for current language
    let translation = this.getNestedTranslation(key, this.translations[lang] || {});
    
    // If not found, fallback to English
    if (!translation) {
      translation = this.getNestedTranslation(key, this.translations['en'] || {});
    }
    
    // If still not found, return key
    if (!translation) {
      return key;
    }
    
    return this.replaceParams(translation, params);
  }
  
  getTranslation(key: string, params?: { [key: string]: string }): Observable<string> {
    return combineLatest([
      this.translationsLoaded$,
      this.currentLanguage$
    ]).pipe(
      map(([loaded, lang]) => {
        if (!loaded) {
          return key;
        }
        
        let translation = this.getNestedTranslation(key, this.translations[lang] || {});
        
        if (!translation) {
          translation = this.getNestedTranslation(key, this.translations['en'] || {});
        }
        
        if (!translation) {
          return key;
        }
        
        return this.replaceParams(translation, params);
      })
    );
  }

  private getNestedTranslation(key: string, translations: Translations): string | null {
    const keys = key.split('.');
    let value: any = translations;
    
    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k];
      } else {
        return null;
      }
    }
    
    return typeof value === 'string' ? value : null;
  }

  private replaceParams(text: string, params?: { [key: string]: string }): string {
    if (!params) return text;
    
    let result = text;
    for (const [key, value] of Object.entries(params)) {
      result = result.replace(new RegExp(`{{${key}}}`, 'g'), value);
    }
    return result;
  }
}

