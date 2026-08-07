import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatSidenav } from '@angular/material/sidenav';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

@Component({
  selector: 'app-pharmacy-layout',
  standalone: false,
  templateUrl: './pharmacy-layout.component.html',
  styleUrl: './pharmacy-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyLayoutComponent implements OnInit {
  @ViewChild(MatSidenav) sidenav?: MatSidenav;

  auth = inject(AuthFacadeService);
  private breakpointObserver = inject(BreakpointObserver);
  private destroyRef = inject(DestroyRef);

  isHandset = signal(false);

  ngOnInit(): void {
    this.breakpointObserver
      .observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.isHandset.set(result.matches);
      });
  }

  closeSidenavOnNavigate(): void {
    if (this.isHandset()) {
      this.sidenav?.close();
    }
  }
}
