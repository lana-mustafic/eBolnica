import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MedicalRecordService } from '../../../../shared/services/medical-record/medical-record.service';
import { inject } from '@angular/core';

@Component({
  selector: 'app-medical-record',
  imports: [],
  standalone: true,
  templateUrl: './medical-record.component.html',
  styleUrl: './medical-record.component.css'
})
export class MedicalRecordComponent implements OnInit{

  private route = inject(ActivatedRoute);
  private service = inject(MedicalRecordService);

  recordNumber: string | null = null;

  ngOnInit(): void {
    const patientId = Number(this.route.snapshot.paramMap.get('patientId'));
    this.service.getMedicalRecord(patientId).subscribe(record =>{
      this.recordNumber = record.recordNumber
    })
  }
}
