import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-prescription-detail',
  standalone: true,
  template: '<div class="container"><h1>Prescription Details</h1><p>Prescription detail component - to be implemented</p><p>Prescription ID: {{ prescriptionId }}</p></div>',
  styles: ['.container { padding: 2rem; }']
})
export class PrescriptionDetailComponent {
  prescriptionId: string | null = null;
  
  constructor(private route: ActivatedRoute) {
    this.route.params.subscribe(params => {
      this.prescriptionId = params['id'] || null;
    });
  }
}
