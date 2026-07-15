import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { GenerationDetailDto, GenerationSummaryDto } from '../../core/models/api.models';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './history.component.html',
  styleUrl: './history.component.scss'
})
export class HistoryComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly items = signal<GenerationSummaryDto[]>([]);
  readonly selected = signal<GenerationDetailDto | null>(null);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ratingValue = 5;
  comments = '';

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.getGenerationHistory().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar el historial.');
        this.loading.set(false);
      }
    });
  }

  open(id: string): void {
    this.api.getGenerationById(id).subscribe({
      next: (detail) => {
        this.selected.set(detail);
        this.ratingValue = detail.rating ?? 5;
        this.comments = '';
      },
      error: () => this.errorMessage.set('No se pudo cargar el detalle.')
    });
  }

  rate(): void {
    const current = this.selected();
    if (!current) return;

    this.api.rateGeneration(current.id, { rating: this.ratingValue, comments: this.comments || null }).subscribe({
      next: () => {
        this.selected.set({ ...current, rating: this.ratingValue });
        this.reload();
      },
      error: () => this.errorMessage.set('No se pudo guardar la calificación.')
    });
  }
}
