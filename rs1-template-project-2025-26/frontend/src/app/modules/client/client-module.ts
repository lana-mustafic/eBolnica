import { NgModule } from '@angular/core';

import { ClientRoutingModule } from './client-routing-module';
import { ClientHomeComponent } from './client-home/client-home.component';
import { SharedModule } from '../shared/shared-module';

@NgModule({
  declarations: [ClientHomeComponent],
  imports: [SharedModule, ClientRoutingModule],
})
export class ClientModule {}
