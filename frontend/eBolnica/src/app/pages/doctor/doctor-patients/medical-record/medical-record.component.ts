import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MedicalRecordService } from '../../../../shared/services/medical-record/medical-record.service';
import { inject } from '@angular/core';
import { MedicalRecord } from '../../../../models/medical-record.dto';
import { CommonModule} from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { FileService, FileInfo } from '../../../../shared/services/file/file.service';
import { saveAs } from 'file-saver';

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

  pdfForm: FormGroup = this.fb.group({
    dateFrom:['', Validators.required],
    dateTo:['', Validators.required]
  });

  medicalRecord?: MedicalRecord;
  recordNumber: string | null = null;
  patientFiles: FileInfo[] = [];
  selectedFile: File | null = null;
  patientId!:number;
  
  showPdfModal = false;
  isGeneratingPdf = false;
  reportCountPreview: number | null = null;


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

    this.pdfForm.valueChanges.subscribe(() => {
    this.updateReportPreview();
  });
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

  formatDateForInput(date: Date): string {
  return date.toISOString().split('T')[0];
  }

  openPdfModal(): void{
    const today = new Date();
    const sixMonthsAgo = new Date();
    sixMonthsAgo.setMonth(today.getMonth()-6);

    this.pdfForm.patchValue({
      dateFrom: this.formatDateForInput(sixMonthsAgo),
      dateTo: this.formatDateForInput(today)
    });

    this.showPdfModal = true;
    this.updateReportPreview();
  }

  closePdfModal(): void {
  this.showPdfModal = false;
  this.reportCountPreview = null;
  }

  updateReportPreview(): void {
  if (!this.pdfForm.valid || !this.medicalRecord?.reports) return;

  const dateFrom = new Date(this.pdfForm.value.dateFrom);
  const dateTo = new Date(this.pdfForm.value.dateTo);

  this.reportCountPreview = this.medicalRecord.reports.filter(r => {
    const d = new Date(r.createdAt);
    return d >= dateFrom && d <= dateTo;
  }).length;
  }

  generatePdf(): void{
    if(this.pdfForm.invalid || !this.medicalRecord) return;

    const dateFrom = this.pdfForm.value.dateFrom;
    const dateTo = this.pdfForm.value.dateTo;

    if(new Date(dateFrom) > new Date(dateTo)){
      alert('Date From must be before Date To');
      return;
    }

    this.isGeneratingPdf = true;
    
    this.service.generatePdf(this.medicalRecord.id, dateFrom, dateTo).subscribe({
      next: (blob) =>{
        saveAs(blob, `MedicalRecord_${this.medicalRecord?.id}_${dateFrom}_${dateTo}.pdf`);
        this.closePdfModal();
      },
      error: () => alert('Error generating PDF'),
      complete: () => this.isGeneratingPdf = false
    })
  }

}
