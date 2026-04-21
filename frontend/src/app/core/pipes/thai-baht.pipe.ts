import { Pipe, PipeTransform } from '@angular/core';

const formatter = new Intl.NumberFormat('th-TH', {
  style: 'currency',
  currency: 'THB',
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
});

/** แสดงราคาแบบเงินบาทไทย (คั่นหลักพัน + สัญลักษณ์ ฿) */
@Pipe({ name: 'thaiBaht', standalone: true })
export class ThaiBahtPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null || Number.isNaN(value)) {
      return '—';
    }
    return formatter.format(value);
  }
}
