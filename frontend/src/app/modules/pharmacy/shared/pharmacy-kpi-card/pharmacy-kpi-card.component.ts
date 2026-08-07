import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PharmacyIconName } from '../pharmacy-icon/pharmacy-icon.component';

export type KpiCardTone = 'purple' | 'green' | 'orange' | 'blue' | 'red';

@Component({
  selector: 'app-pharmacy-kpi-card',
  standalone: false,
  templateUrl: './pharmacy-kpi-card.component.html',
  styleUrl: './pharmacy-kpi-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyKpiCardComponent {
  icon = input.required<PharmacyIconName>();
  title = input.required<string>();
  value = input.required<string | number>();
  subtitle = input<string>();
  trendLabel = input<string>();
  trendPositive = input(true);
  tone = input<KpiCardTone>('purple');
}
