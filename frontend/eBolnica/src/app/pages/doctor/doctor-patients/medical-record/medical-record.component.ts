import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MedicalRecordService } from '../../../../shared/services/medical-record/medical-record.service';
import { inject } from '@angular/core';
import { MedicalRecord } from '../../../../models/medical-record.dto';
import { CommonModule} from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-medical-record',
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  standalone: true,
  templateUrl: './medical-record.component.html',
  styleUrl: './medical-record.component.css'
})
export class MedicalRecordComponent implements OnInit{

  private route = inject(ActivatedRoute);
  private service = inject(MedicalRecordService);
  private fb = inject(FormBuilder);

  reportForm: FormGroup = this.fb.group({
    symptoms:['',Validators.required],
    diagnosis:['',Validators.required],
    therapy:['',Validators.required],
    description:['']
  });

  medicalRecord?: MedicalRecord;
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

  onSubmitReport():void{
    if(this.reportForm.valid && this.medicalRecord){
      const reportData = {
        medicalRecordId: this.medicalRecord.id,
        symptoms: this.reportForm.value.symptoms,
        diagnosis: this.reportForm.value.diagnosis,
        therapy: this.reportForm.value.therapy,
        description: this.reportForm.value.description
      };

      this.service.newMedicalReport(reportData).subscribe({
        next: (response) => {
          console.log('Report created successfully', response);
          this.resetForm();
        },
        error: (err) =>{
          console.error('Error creating report', err);
        }
      });
    }
  }

  resetForm(): void{
    this.reportForm.reset();
  }

}
