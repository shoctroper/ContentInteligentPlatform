import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { GenerationDetailDto, OutputFormat, ProfileDto } from '../../core/models/api.models';

@Component({
  selector: 'app-generate',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './generate.component.html',
  styleUrl: './generate.component.scss'
})
export class GenerateComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly profiles = signal<ProfileDto[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly result = signal<GenerationDetailDto | null>(null);

  readonly outputFormats: OutputFormat[] = ['TikTok', 'YouTubeShorts', 'InstagramReel', 'Podcast'];

  sourceText = '';
  profileSlug = '';
  outputFormat: OutputFormat = 'TikTok';

  ngOnInit(): void {
    this.api.getProfiles().subscribe({
      next: (profiles) => {
        this.profiles.set(profiles);
        if (profiles.length > 0) this.profileSlug = profiles[0].slug;
      },
      error: () => this.errorMessage.set('No se pudieron cargar los perfiles. ¿Está la Api corriendo?')
    });
  }

  submit(): void {
    if (!this.sourceText.trim() || !this.profileSlug) return;

    this.loading.set(true);
    this.errorMessage.set(null);
    this.result.set(null);

    this.api.generateScript({
      sourceText: this.sourceText,
      profileSlug: this.profileSlug,
      outputFormat: this.outputFormat
    }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.detail ?? 'Error generando el guion.');
        this.loading.set(false);
      }
    });
  }
}
