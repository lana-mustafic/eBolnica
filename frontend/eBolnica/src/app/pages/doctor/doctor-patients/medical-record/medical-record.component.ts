import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MedicalRecordService } from '../../../../shared/services/medical-record/medical-record.service';
import { inject } from '@angular/core';
import { MedicalRecord } from '../../../../models/medical-record.dto';
import { CommonModule} from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { FileService, FileInfo } from '../../../../shared/services/file/file.service';

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
  private fileService = inject(FileService);

  reportForm: FormGroup = this.fb.group({
    symptoms:['',Validators.required],
    diagnosis:['',Validators.required],
    therapy:['',Validators.required],
    description:['']
  });

  medicalRecord?: MedicalRecord;
  recordNumber: string | null = null;
  patientFiles: FileInfo[] = [];
  selectedFile: File | null = null;
  patientId!:number;

  ngOnInit(): void {
    this.patientId = Number(this.route.snapshot.paramMap.get('patientId'));
    
    this.service.getMedicalRecord(this.patientId).subscribe({next:(data)=>{
      this.medicalRecord = data;
      console.log('Medical Record:', data)
    },
    error: (err)=>{
      console.error('Error', err);
    }})

    this.loadFiles();
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

  loadFiles(): void{
    this.fileService.getPatientFiles(this.patientId).subscribe(files=>this.patientFiles = files);
  }

  onFileSelected(event: Event): void{
    const input = event.target as HTMLInputElement;
    if(input.files && input.files.length >0){
      this.selectedFile = input.files[0];
    }
  }

  uploadDocument(): void{
    if(!this.selectedFile)
      return;

    this.fileService.uploadFile(this.selectedFile, this.patientId).subscribe(()=>{
      this.selectedFile = null;
      this.loadFiles();
    })
  }

  downloadFile(fileId:number):void{
    this.fileService.downloadFile(fileId).subscribe(blob=>{
      const url = window.URL.createObjectURL(blob);
      window.open(url);
    })
  }

  deleteFile(fileId:number):void{
    if(!confirm('Are you sure you want to delete this document?')) return;

    this.fileService.deleteFile(fileId).subscribe(()=>{
      this.loadFiles();
    })
  }

}
