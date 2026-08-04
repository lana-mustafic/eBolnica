import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';

export interface NotificationOptions {
  duration?: number;
  position?: 'top-left' | 'top-right' | 'bottom-left' | 'bottom-right';
  icon?: string;
  actionText?: string;
  onAction?: () => void;
}

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'info' | 'warning';
  title: string;
  message: string;
  options?: NotificationOptions;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private notifications$ = new Subject<Notification>();
  private notificationQueue: Notification[] = [];
  private isShowingNotification = false;

  /**
   * Observable stream of notifications
   */
  getNotifications(): Observable<Notification> {
    return this.notifications$.asObservable();
  }

  /**
   * Show success notification
   */
  success(title: string, message: string, options?: NotificationOptions): void {
    const notification: Notification = {
      id: this.generateId(),
      type: 'success',
      title,
      message,
      options: {
        duration: 5000,
        position: 'bottom-right',
        icon: 'check-circle',
        ...options
      },
      timestamp: new Date()
    };

    this.queueNotification(notification);
  }

  /**
   * Show error notification
   */
  error(title: string, message: string, options?: NotificationOptions): void {
    const notification: Notification = {
      id: this.generateId(),
      type: 'error',
      title,
      message,
      options: {
        duration: 8000,
        position: 'bottom-right',
        icon: 'exclamation-triangle',
        ...options
      },
      timestamp: new Date()
    };

    this.queueNotification(notification);
  }

  /**
   * Show info notification
   */
  info(title: string, message: string, options?: NotificationOptions): void {
    const notification: Notification = {
      id: this.generateId(),
      type: 'info',
      title,
      message,
      options: {
        duration: 5000,
        position: 'bottom-right',
        icon: 'info-circle',
        ...options
      },
      timestamp: new Date()
    };

    this.queueNotification(notification);
  }

  /**
   * Show warning notification
   */
  warning(title: string, message: string, options?: NotificationOptions): void {
    const notification: Notification = {
      id: this.generateId(),
      type: 'warning',
      title,
      message,
      options: {
        duration: 6000,
        position: 'bottom-right',
        icon: 'exclamation-circle',
        ...options
      },
      timestamp: new Date()
    };

    this.queueNotification(notification);
  }

  /**
   * Queue notification for display
   */
  private queueNotification(notification: Notification): void {
    this.notificationQueue.push(notification);
    this.processQueue();
  }

  /**
   * Process notification queue
   */
  private processQueue(): void {
    if (this.isShowingNotification || this.notificationQueue.length === 0) {
      return;
    }

    this.isShowingNotification = true;
    const notification = this.notificationQueue.shift()!;

    // Emit notification
    this.notifications$.next(notification);

    // After duration, process next
    const duration = notification.options?.duration || 5000;
    setTimeout(() => {
      this.isShowingNotification = false;
      this.processQueue();
    }, duration);
  }

  /**
   * Generate unique ID for notification
   */
  private generateId(): string {
    return `notification-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }
}
