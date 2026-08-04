import { inject, Injectable } from '@angular/core';
import { HttpClient} from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface FileInfo {
  id: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: Date;
  patientId: number;
}

@Injectable({
  providedIn: 'root'
})
export class FileService {

  private apiUrl = environment.apiUrl + '/file';

  private http = inject(HttpClient);

  uploadFile(file: File, patientId: number): Observable<any>{
    const formData = new FormData();
    formData.append('file', file);
    formData.append('patientId', patientId.toString());

    return this.http.post(this.apiUrl+'/upload', formData);
  };

  getPatientFiles(patientId:number): Observable<FileInfo[]>{
    return this.http.get<FileInfo[]>(`${this.apiUrl}/patient/${patientId}`);
  }

  downloadFile(fileId:number): Observable<Blob>{
    return this.http.get(`${this.apiUrl}/download/${fileId}`, {responseType: 'blob'});
  }

  deleteFile(fileId:number): Observable<any>{
    return this.http.delete(`${this.apiUrl}/${fileId}`);
  }
}
