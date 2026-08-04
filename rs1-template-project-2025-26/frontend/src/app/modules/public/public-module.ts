import { NgModule } from '@angular/core';

import { PublicRoutingModule } from './public-routing-module';
import { PublicLayoutComponent } from './public-layout/public-layout.component';
import { PublicHomeComponent } from './public-home/public-home.component';
import { SharedModule } from '../shared/shared-module';

@NgModule({
  declarations: [PublicLayoutComponent, PublicHomeComponent],
  imports: [SharedModule, PublicRoutingModule],
})
export class PublicModule {}
