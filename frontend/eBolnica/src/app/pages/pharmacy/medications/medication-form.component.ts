import { Component } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';

@Component({
  selector: 'app-medication-form',
  standalone: true,
  imports: [RouterModule],
  template: '<div class="container"><h1>{{ isEdit ? "Edit Medication" : "New Medication" }}</h1><p>Medication form component - to be implemented</p></div>',
  styles: ['.container { padding: 2rem; }']
})
export class MedicationFormComponent {
  isEdit = false;
  
  constructor(private route: ActivatedRoute) {
    this.route.params.subscribe(params => {
      this.isEdit = !!params['id'];
    });
  }
}
