import { Component, Input } from '@angular/core';
import { PharmacyIconName } from '../pharmacy-icon/pharmacy-icon.component';

export type KpiCardTone = 'purple' | 'green' | 'orange' | 'blue' | 'red';

@Component({
  selector: 'app-pharmacy-kpi-card',
  standalone: false,
  templateUrl: './pharmacy-kpi-card.component.html',
  styleUrl: './pharmacy-kpi-card.component.scss',
})
export class PharmacyKpiCardComponent {
  @Input({ required: true }) icon!: PharmacyIconName;
  @Input({ required: true }) title!: string;
  @Input({ required: true }) value!: string | number;
  @Input() subtitle?: string;
  @Input() trendLabel?: string;
  @Input() trendPositive = true;
  @Input() tone: KpiCardTone = 'purple';
}
