import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { AuditEvent } from '../core/models/models';
import { AuditApiService } from '../core/services/audit-api.service';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './audit.component.html'
})
export class AuditComponent implements OnInit {
  private readonly auditApi = inject(AuditApiService);

  readonly pageSize = 20;
  page = 1;
  events: AuditEvent[] = [];
  loading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadPage();
  }

  loadPage(): void {
    this.loading = true;
    this.errorMessage = '';
    this.auditApi.getPage(this.page, this.pageSize).subscribe({
      next: response => {
        this.events = response.items;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Could not load audit events.';
      }
    });
  }

  previousPage(): void {
    if (this.page === 1) {
      return;
    }

    this.page -= 1;
    this.loadPage();
  }

  nextPage(): void {
    if (this.events.length < this.pageSize) {
      return;
    }

    this.page += 1;
    this.loadPage();
  }
}
