using System;

namespace DateRepeater
{
class Program
{
	public static void Main(string[] args)
	{
		Console.WriteLine("Hello World!");
		
		EventRepeatParser erp = new EventRepeatParser();
		erp.Input = "repeat weekly every(1)[monday,wednesday,friday] {start(2010-6-11) end(2010-7-2)}";
		erp.Parse();
		
		Console.WriteLine("Input : " + erp.Input);
		Console.WriteLine("English parse : " + erp.ToEnglish());
		Console.WriteLine();
		Console.WriteLine("Result dates : ");
		foreach (DateTime date in erp.GenerateDateList()) {
			Console.WriteLine(date.ToString());
		}
		
		Console.Write("Press any key to continue . . . ");
		Console.ReadKey(true);
	}
}
}