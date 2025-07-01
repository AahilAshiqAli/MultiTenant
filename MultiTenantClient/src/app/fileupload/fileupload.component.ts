import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Subscription } from 'rxjs';
import { LogService } from '../shared/services/log.service';
import { ProgressService } from '../shared/services/progress.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-fileupload',
  standalone: true,
  imports: [CommonModule, FormsModule , ReactiveFormsModule],
  templateUrl: './fileupload.component.html',
  styles: ''
})
export class FileuploadComponent implements OnInit {
  selectedFiles: File[] = [];
  progressInfos: { file: File; progress: number; sub: Subscription | null; privacyControl: FormControl<boolean>; }[] = [];
  totalBytes: number = 0;
  videoResolutions: { [key: string]: { width: number; height: number } } = {};
  uploadedSize: number = 0;
  conversionProgress: { [key:string] : number} = {};
  tenantLogs: any[] = [];
  validationError: string = '';
  private readonly MAX_SIZE = 2 * 1024 * 1024 * 1024;
  showLogs: boolean = false;
  videoResolution: any = null;
  allowSubmit: boolean = false;
  uploadResponse: any = null;
  showConversionProgress: boolean = false;


  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  constructor(
    private http: HttpClient,
    private logService: LogService,
    private progressService: ProgressService,
    private toastr: ToastrService
  ) {
  }

  ngOnInit(): void {
    this.logService.startConnection(); 


    this.logService.log$.subscribe((log: any) => {
      this.tenantLogs.push(log);
      // Optionally auto-scroll to latest
      setTimeout(() => {
        const logBox = document.getElementById('logBox');
        if (logBox) {
          logBox.scrollTop = logBox.scrollHeight;
        }
      }, 100);
    });
  }

  toggleLogs() {
    this.showLogs = !this.showLogs;
  }

  openFileBrowser() {
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files) return;
  
    this.validationError = '';
    this.uploadedSize = 0;
    this.selectedFiles = [];
  
    const files = Array.from(input.files);
    this.progressInfos = [];
  
    for (const file of files) {
      if (!/^(audio|video)\//.test(file.type)) {
        this.validationError += `Invalid file type: ${file.name}\n`;
        continue;
      }
  
      if (file.size > this.MAX_SIZE) {
        this.validationError += `File too large: ${file.name}. Max size is ${this.formatBytes(this.MAX_SIZE)}\n`;
        continue;
      }
  
      const fileType = file.type.split('/')[0];
  
      if (fileType === 'video') {
        const video = document.createElement('video');
        video.preload = 'metadata';
  
        video.onloadedmetadata = () => {
          const width = video.videoWidth;
          const height = video.videoHeight;
          console.log(`Video resolution for ${file.name}: ${width}x${height}`);
  
          // Store metadata per file if needed
          this.videoResolutions[file.name] = { width, height };
        };
  
        video.onerror = () => {
          this.validationError += `Unable to read metadata for video: ${file.name}\n`;
        };
  
        video.src = URL.createObjectURL(file);
      }
  
      if (fileType === 'audio') {
        this.allowSubmit = true;
      }
  
      this.totalBytes += file.size;
      this.selectedFiles.push(file);
      this.progressInfos.push({ file, progress: 0, sub: null , privacyControl: new FormControl<boolean>(false, { nonNullable: true }) });
    }
  
    if (this.validationError) {
      // optionally clear files if any invalids exist
      console.warn('Validation errors:', this.validationError);
    }
  }
  

  uploadFiles(): void {
    this.progressInfos.forEach((info, index) => {
      const file = info.file;
      const originalFileName = file.name;
      const contentType = file.type;

      this.http.get<any>(`${environment.apiBaseUrl}/Contents/upload-url`, {
        params: { fileName: originalFileName }
      }).subscribe({
        next: (res) => {
          const { uploadUrl, blobFileName } = res;

          const req = this.http.put(uploadUrl, file, {
            headers: {
              'x-ms-blob-type': 'BlockBlob',
              'Content-Type': contentType
            },
            reportProgress: true,
            observe: 'events'
          });

          this.showConversionProgress = true;

          const sub = req.subscribe({
            next: (event: HttpEvent<any>) => {
              if (event.type === HttpEventType.UploadProgress && event.total) {
                info.progress = Math.round((event.loaded / event.total) * 100);
              } else if (event.type === HttpEventType.Response) {
                const payload = {
                  originalFileName,
                  contentType,
                  size: file.size,
                  rendition: "480p",
                  blobFileName,
                  isPrivate: info.privacyControl.value
                };
                console.log(payload);
                this.http.post(`${environment.apiBaseUrl}/Contents`, payload).subscribe({
                  next: (res) => {
                    this.uploadResponse = res; // ✅ Capture the response
                    this.toastr.success('Upload initiated successfully', 'Success');
                    this.progressService.startPolling((progress) => {
                      this.conversionProgress[originalFileName] = progress;
                    });
                  },
                  error: (err) => {
                    this.toastr.error('Upload notification failed', 'Error');
                    console.error(err);
                  }
                });

              }
            },
            error: (err) => {
              this.toastr.error(`Failed upload: ${file.name}`);
              console.error(err);
            }
          });

          info.sub = sub;
        },
        error: (err) => {
          this.toastr.error('Upload URL error');
          console.error(err);
        }
      });
    });
  }

  cancelUpload(index: number) {
    const info = this.progressInfos[index];
    if (info?.sub) {
      info.sub.unsubscribe();
      info.sub = null;
      info.progress = 0;
    }
    this.progressInfos.splice(index, 1);
    this.selectedFiles.splice(index, 1);
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}
