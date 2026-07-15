import { Routes } from '@angular/router';
import { GenerateComponent } from './features/generate/generate.component';
import { HistoryComponent } from './features/history/history.component';

export const routes: Routes = [
  { path: '', redirectTo: 'generar', pathMatch: 'full' },
  { path: 'generar', component: GenerateComponent },
  { path: 'historial', component: HistoryComponent }
];
