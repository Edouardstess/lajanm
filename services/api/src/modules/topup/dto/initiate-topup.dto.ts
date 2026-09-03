import { IsInt, Min } from 'class-validator';

export class InitiateTopupDto {
  // Whole HTG (gourdes), not centimes — the client shouldn't need to know
  // the ledger's minor-unit representation.
  @IsInt()
  @Min(25)
  amountHTG: number;
}
