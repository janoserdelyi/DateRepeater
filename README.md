# DateRepeater

Generate a list of dates based on repetition patterns you define

## History

This is a rather ancient - though still useful - bit of code from late 2006.

I have many many libraries i've made over the years that i will will eventually be adding to github from my old subversion repo

## Example usage

This is the guts for a small "Hello, World" style program to test : 

```
EventRepeatParser erp = new EventRepeatParser();
erp.Input = "repeat weekly every(1)[monday,wednesday,friday] {start(2018-09-09) end(2018-09-23)}";
erp.Parse();

Console.WriteLine("Input : " + erp.Input);
Console.WriteLine("English parse : " + erp.ToEnglish());
Console.WriteLine();
Console.WriteLine("Result dates : ");
foreach (DateTime date in erp.GenerateDateList()) {
	Console.WriteLine(date.ToString());
}
```

Other example patterns : 

```
Daily:
"repeat daily every 2 days. start on 2006-11-14 and end on 2006-12-16"
repeat daily every(2) {start(2006-11-14) end(2006-12-16)}

"repeat daily every day. start on 2006-11-14 and repeat 20 times"
repeat daily every(0) {start(2006-11-14) occur(20)}

Weekly:
"repeat weekly every second week on thursday and saturday"
repeat weekly every(2)[Thursday,Saturday] {}

Monthly:
"repeat monthly every 2nd month on the 12th day..." note: start(end|occur) syntax the same from now on.
repeat monthly every(2) day(12) {}

"repeat monthly every 2nd month on the 2nd Thursday..."
repeat monthly every(2) the(2)[Thursday] {}

Yearly:
"repeat yearly every October 12..."
repeat yearly every(10) day(12) {}

"repeat yearly every second Thursday of October"
repeat yearly every(10) the(2)[Thursday] {}
```

