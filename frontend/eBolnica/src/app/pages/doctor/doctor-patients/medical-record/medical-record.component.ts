import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MedicalRecordService } from '../../../../shared/services/medical-record/medical-record.service';
import { inject } from '@angular/core';
import { MedicalRecordDto } from '../../../../models/medical-record.dto';
import { CommonModule} from '@angular/common';

@Component({
  selector: 'app-medical-record',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './medical-record.component.html',
  styleUrl: './medical-record.component.css'
})
export class MedicalRecordComponent implements OnInit{

  private route = inject(ActivatedRoute);
  private service = inject(MedicalRecordService);

  medicalRecord?: MedicalRecordDto;

  recordNumber: string | null = null;

  ngOnInit(): void {
    const patientId = Number(this.route.snapshot.paramMap.get('patientId'));
    
    this.service.getMedicalRecord(patientId).subscribe({next:(data)=>{
      this.medicalRecord = data;
      console.log('Medical Record:', data)
    },
    error: (err)=>{
      console.error('Error', err);
    }})
  }
}
