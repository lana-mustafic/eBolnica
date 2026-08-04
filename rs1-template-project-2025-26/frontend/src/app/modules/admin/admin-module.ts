import { NgModule } from '@angular/core';

import { AdminRoutingModule } from './admin-routing-module';
import { AdminLayoutComponent } from './admin-layout/admin-layout.component';
import { AdminSettingsComponent } from './admin-settings/admin-settings.component';
import { AdminUsersComponent } from './users/admin-users.component';
import { SharedModule } from '../shared/shared-module';

@NgModule({
  declarations: [AdminLayoutComponent, AdminSettingsComponent, AdminUsersComponent],
  imports: [AdminRoutingModule, SharedModule],
})
export class AdminModule {}
