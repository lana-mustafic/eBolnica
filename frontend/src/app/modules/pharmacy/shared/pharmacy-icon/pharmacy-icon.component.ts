import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type PharmacyIconName =
  | 'pill'
  | 'package'
  | 'clipboard'
  | 'wallet'
  | 'trending-up'
  | 'triangle-alert'
  | 'calendar'
  | 'bell'
  | 'settings'
  | 'layout-dashboard'
  | 'plus'
  | 'log-out'
  | 'check-circle'
  | 'file-text'
  | 'activity'
  | 'search'
  | 'download'
  | 'upload'
  | 'pencil'
  | 'trash-2'
  | 'more-vertical'
  | 'clock'
  | 'eye'
  | 'user'
  | 'phone'
  | 'printer'
  | 'save';

@Component({
  selector: 'app-pharmacy-icon',
  standalone: false,
  templateUrl: './pharmacy-icon.component.html',
  styleUrl: './pharmacy-icon.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyIconComponent {
  name = input.required<PharmacyIconName>();
  size = input(20);
  strokeWidth = input(2);
}
