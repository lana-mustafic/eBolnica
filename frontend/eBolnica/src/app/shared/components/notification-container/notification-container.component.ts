import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService, Notification } from '../../services/notification.service';
import { NotificationToastComponent } from '../notification-toast/notification-toast.component';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-notification-container',
  standalone: true,
  imports: [CommonModule, NotificationToastComponent],
  templateUrl: './notification-container.component.html',
  styleUrl: './notification-container.component.css'
})
export class NotificationContainerComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];
  private destroy$ = new Subject<void>();

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    this.notificationService.getNotifications()
      .pipe(takeUntil(this.destroy$))
      .subscribe(notification => {
        this.notifications.push(notification);
        
        // Auto-remove after duration
        const duration = notification.options?.duration || 5000;
        setTimeout(() => {
          this.removeNotification(notification.id);
        }, duration);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  removeNotification(id: string): void {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }

  onDismiss(notification: Notification): void {
    this.removeNotification(notification.id);
  }

  onAction(notification: Notification): void {
    // Action handled in toast component
    this.removeNotification(notification.id);
  }
}
