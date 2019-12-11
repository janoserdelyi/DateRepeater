using System;
using DateRepeater;

namespace DateRepeaterTest
{
	public class Program
	{
		public static void Main (string[] args) {
			Console.WriteLine ("Hello World!");

			EventRepeatParser erp = new EventRepeatParser ();

			erp.Input = "repeat weekly every(1)[monday,wednesday,friday] {start(2018-09-09) end(2018-09-23)}";
			erp.Parse ();
			ExtendedToString (erp);

			// "repeat daily every 2 days. start on x and end on y"
			erp.Input = "repeat daily every(2) {start(2018-09-01) end(2018-09-15)}";
			erp.Parse ();
			ExtendedToString (erp);

			// "repeat weekly every second week on thursday and saturday. start on x. gimme 12 of them" note it's 12 groupings - so 24 dates
			erp.Input = "repeat weekly every(2)[Thursday,Saturday] {start(2018-09-17) occur(12)}";
			erp.Parse ();
			ExtendedToString (erp);

			// "repeat monthly every 2nd month on the 2nd Thursday..."
			erp.Input = "repeat monthly every(2) the(2)[Thursday] {start(2018-09-17) occur(4)}";
			erp.Parse ();
			ExtendedToString (erp);

			if (!Console.IsInputRedirected) {
				Console.Write ("Press any key to continue . . . ");
				Console.ReadKey (true);
			}
		}

		public static void ExtendedToString (
			EventRepeatParser erp
		) {
			if (erp == null) {
				Console.WriteLine ("nope");
			}

			Console.WriteLine ();
			Console.WriteLine ("Input : " + erp.Input);
			Console.WriteLine ("English parse : " + erp.ToEnglish ());
			Console.WriteLine ();
			Console.WriteLine ("Result dates : ");
			foreach (DateTime date in erp.GenerateDateList ()) {
				Console.WriteLine (date.ToString ("yyyy-MM-dd") + " - " + date.DayOfWeek.ToString ());
			}
			Console.WriteLine ();
			Console.WriteLine ("-".PadRight (80, '-'));
		}
	}
}
