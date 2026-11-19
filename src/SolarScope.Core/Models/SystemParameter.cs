namespace SolarScope.Core.Models;

public class SystemParameter
{
    public double SystemCapacityKwp {get; set;}
    public double PerformanceRatio {get; set;}= 0.75;
    public (bool IsValid, string? err) Validate(){
        if (!double.IsFinite(SystemCapacityKwp) || SystemCapacityKwp <= 0){
            return (false, "Kapasitas sistem (kWp) harus lebih besar dari 0");
        }
        if (!double.IsFinite(PerformanceRatio) || PerformanceRatio < 0.01 || PerformanceRatio > 1.00){
            return (false, "Performance ratio harus bernilai antara 0.01 dan 1.00");
        }
        return (true, null);
    }
}
