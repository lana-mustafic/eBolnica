import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Notification } from '../../services/notification.service';

@Component({
  selector: 'app-notification-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-toast.component.html',
  styleUrl: './notification-toast.component.css'
})
export class NotificationToastComponent implements OnInit, OnDestroy {
  @Input() notification!: Notification;
  @Output() dismiss = new EventEmitter<void>();
  @Output() action = new EventEmitter<void>();

  private timeoutId?: any;

  ngOnInit(): void {
    const duration = this.notification.options?.duration || 5000;
    
    // Auto-dismiss after duration
    this.timeoutId = setTimeout(() => {
      this.dismiss.emit();
    }, duration);
  }

  ngOnDestroy(): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }
  }

  onDismiss(): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }
    this.dismiss.emit();
  }

  onAction(): void {
    if (this.notification.options?.onAction) {
      this.notification.options.onAction();
    }
    this.action.emit();
    this.onDismiss();
  }

  getIconClass(): string {
    const iconMap: { [key: string]: string } = {
      'check-circle': '✓',
      'exclamation-triangle': '⚠',
      'exclamation-circle': '⚠',
      'info-circle': 'ℹ'
    };
    return iconMap[this.notification.options?.icon || 'info-circle'] || 'ℹ';
  }

  getPositionClass(): string {
    return `position-${this.notification.options?.position || 'bottom-right'}`;
  }

  /**
   * Get ARIA label for notification
   */
  getAriaLabel(): string {
    return `${this.notification.type} notification: ${this.notification.title}. ${this.notification.message}`;
  }
}
