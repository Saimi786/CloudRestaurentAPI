namespace CloudRestaurent.Domain.Companies;

public enum ReceiptTemplate
{
    Compact = 0,   // 80mm thermal — minimal, dense, fast print
    Classic = 1    // A4/Letter — full header, columnar lines, footer block
}
