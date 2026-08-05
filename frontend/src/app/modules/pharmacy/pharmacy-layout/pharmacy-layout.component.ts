import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { MatSidenav } from '@angular/material/sidenav';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

@Component({
  selector: 'app-pharmacy-layout',
  standalone: false,
  templateUrl: './pharmacy-layout.component.html',
  styleUrl: './pharmacy-layout.component.scss',
})
export class PharmacyLayoutComponent implements OnInit {
  @ViewChild(MatSidenav) sidenav?: MatSidenav;

  auth = inject(AuthFacadeService);
  private breakpointObserver = inject(BreakpointObserver);

  isHandset = false;

  ngOnInit(): void {
    this.breakpointObserver.observe([Breakpoints.Handset, Breakpoints.TabletPortrait]).subscribe((result) => {
      this.isHandset = result.matches;
    });
  }

  closeSidenavOnNavigate(): void {
    if (this.isHandset) {
      this.sidenav?.close();
    }
  }
}
