namespace Tutorial7.Models;

public class Doctor
{
    public int    IdDoctor         { get; set; }
    public int    IdSpecialization { get; set; }
    public string FirstName        { get; set; } = string.Empty;
    public string LastName         { get; set; } = string.Empty;
    public string LicenseNumber    { get; set; } = string.Empty;
    public bool   IsActive         { get; set; }
}