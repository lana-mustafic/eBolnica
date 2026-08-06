import { Component, Input } from '@angular/core';

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
  | 'printer';

@Component({
  selector: 'app-pharmacy-icon',
  standalone: false,
  templateUrl: './pharmacy-icon.component.html',
  styleUrl: './pharmacy-icon.component.scss',
})
export class PharmacyIconComponent {
  @Input({ required: true }) name!: PharmacyIconName;
  @Input() size = 20;
  @Input() strokeWidth = 2;
}
