namespace Tutorial7.Models;

public class Patient
{
    public int      IdPatient    { get; set; }
    public string   FirstName    { get; set; } = string.Empty;
    public string   LastName     { get; set; } = string.Empty;
    public string   Email        { get; set; } = string.Empty;
    public string   PhoneNumber  { get; set; } = string.Empty;
    public DateTime DateOfBirth  { get; set; }
    public bool     IsActive     { get; set; }
}