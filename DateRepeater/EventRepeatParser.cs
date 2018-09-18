using System;
using System.Collections;
using scg = System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DateRepeater
{
	public class EventRepeatParser
	{
		/*	2006 11 14 janos erdelyi
			prelim and intended usage.
		
			this is text parser for daily, weekly, monthly, and yearly repeating events
			the mass of tiny data bits it takes to represent this is in a tradional 
			relational database structure is pretty overbearing for the end result.
			additionaly, the data integrity benefits of relational structures offer no 
			benefit, but large overhead.
		
			here are some examples of the structure i'm thinking about
		
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
		*/

		public EventRepeatParser () {
			initGuts();
		}
	
		// when reusing an EventRepeatParser object i found it was not cleaning itself up from previous uses
		private void initGuts () {
			timeUnit = TimeUnit.Unknown;
			everyNumber = 0;
			dayNumber = 0;
			theNumber = 0;
			occurNumber = 0;
			dayArray = new string[0];
			startDt = new DateTime(0);
			endDt = null;
			hasBeenParsed = false;
		}

		public void Parse() {

			initGuts();

			//doh. with the advent of allowing piecemeal input... i need to kill this requirement.
			if (input == null) {
			//throw new Exception("Error, no input provided.");
			}
		
			//save aside a version of the input;
			string minput = input; //minput = manipulated input
		
			//normalize everything first.
			minput = minput.ToLower();
		
			//all strings should begin with "repeat (daily|weekly|monthly|yearly) "
			//
			if (minput.Substring(0,6) != "repeat") {
				throw new Exception("Error, malformed input. \"" + input + "\"");
			}
		
			minput = minput.Substring(7);
			int nextSpace = minput.IndexOf(" ");
		
			switch (minput.Substring(0,nextSpace)) {
				case "daily" :
					timeUnit = TimeUnit.Daily;
					break;
				case "weekly" :
					timeUnit = TimeUnit.Weekly;
					break;
				case "monthly" :
					timeUnit = TimeUnit.Monthly;
					break;
				case "yearly" :
					timeUnit = TimeUnit.Yearly;
					break;
			}
		
			if (timeUnit == TimeUnit.Unknown) {
				throw new Exception("Unable to determine unit of time. \"" + minput.Substring(0,nextSpace) + "\" is not a valid input. (daily|weekly|monthly|yearly)");
			}
		
			minput = minput.Substring(nextSpace+1);
		
			//the next bit should be "every(<digit>)"
		
			string everyReg = @"every\((?<everynumber>\d{1,})\)";
			Regex re = new Regex(everyReg, RegexOptions.IgnoreCase);
		
			if (re.IsMatch(minput)) {
				everyNumber = Convert.ToInt32(re.Match(minput).Groups["everynumber"].Value);
			}
			re = null;
		
			//strip minput down further
			minput = minput.Substring(minput.IndexOf(')')+1);
		
			//the next section gets a little more complicated, 
			//so i'm going to do that AFTER i take care of the repeat ranges, which is 
			//consistent across the board
		
			string rangeReg = @"\{(?<range>.+?)\}";
			string rangeInput = null;
			re = new Regex(rangeReg, RegexOptions.IgnoreCase);
			if (re.IsMatch(minput)) {
				rangeInput = re.Match(minput).Groups["range"].Value;
			} else {
				throw new Exception("Error, unable to determine recurrance range.");
			}
			re = null;
		
			//clean up the minput - remove the range recurrance stuff since we've saved it in another string
			//this could throw an exception if the input is malformed
			minput = minput.Substring(0, minput.IndexOf('{')-1);
		
			//dig into the range recurrance
			string startDtReg = @"start\((?<startdt>.+?)\)";
			re = new Regex(startDtReg, RegexOptions.IgnoreCase);
			string start = null;
			if (re.IsMatch(rangeInput)) {
				start = re.Match(rangeInput).Groups["startdt"].Value;
			}
			re = null;
			
			// considering changing it so that a missing start date means 'now'
			if (start == null || start == "") {
				throw new Exception("Unable to find a start date. Possible malformed entry.");
			}

			// holy crap this is old. refactor with tryparse
			string[] startSplit = start.Split('-');
			try {
				startDt = new DateTime(
					Convert.ToInt32(startSplit[0]),
					Convert.ToInt32(startSplit[1]),
					Convert.ToInt32(startSplit[2])
				);
			} catch (Exception oops) {
				throw new Exception("Error determining Start Date. input \"" + start + "\". " + oops.Message);
			}
		
			//next, there could be an end date or an occurance counter
			if (rangeInput.Contains("end(")) {
				string endDtReg = @"end\((?<enddt>.+?)\)";
				re = new Regex(endDtReg, RegexOptions.IgnoreCase);
				string end = null;
				if (re.IsMatch(rangeInput)) {
					end = re.Match(rangeInput).Groups["enddt"].Value;
				}
				re = null;
			
				if (end == null || end == "") {
					throw new Exception("Unable to find a end date. Possible malformed entry.");
				}
				string[] endSplit = end.Split('-');
				try {
					//this can bomb on something like 208-04-31 which is not a valid day
					//get day 1 then check the month range
					int daysInMonth = DateTime.DaysInMonth(Convert.ToInt32(endSplit[0]), Convert.ToInt32(endSplit[1]));
					int targetDayNumber = Convert.ToInt32(endSplit[2]);
					if (targetDayNumber > daysInMonth) {
						targetDayNumber = daysInMonth;
					}
					endDt = new DateTime(
						Convert.ToInt32(endSplit[0]),
						Convert.ToInt32(endSplit[1]),
						targetDayNumber
					);
				} catch (Exception oops) {
					throw new Exception("Error determining End Date. input \"" + end + "\". " + oops.Message);
				}
			}
		
			if (rangeInput.Contains("occur(")) {
				string occurReg = @"occur\((?<occur>\d{1,})\)";
				re = new Regex(occurReg, RegexOptions.IgnoreCase);
			
				if (re.IsMatch(rangeInput)) {
					occurNumber = Convert.ToInt32(re.Match(rangeInput).Groups["occur"].Value);
				}
				re = null;
			}
		
			//now we're back to jolly times with the minput
			//this is the most variable section
			if (minput.Contains("day(")) {
				re = new Regex(@"day\((?<day>\d{1,})\)", RegexOptions.IgnoreCase);
				if (re.IsMatch(minput)) {
					dayNumber = Convert.ToInt32(re.Match(minput).Groups["day"].Value);
				}
				re = null;
			}
		
			if (minput.Contains("the(")) {
				re = new Regex(@"the\((?<the>\d{1,})\)", RegexOptions.IgnoreCase);
				if (re.IsMatch(minput)) {
					theNumber = Convert.ToInt32(re.Match(minput).Groups["the"].Value);
				}
				re = null;
			}
		
			if (minput.Contains("[") && minput.Contains("]")) {
				string days = null;
				re = new Regex(@"\[(?<days>.+?)\]", RegexOptions.IgnoreCase);
				if (re.IsMatch(minput)) {
					days = re.Match(minput).Groups["days"].Value;
				}
				re = null;
			
				dayArray = days.Split(',');
			}
		
			hasBeenParsed = true;
		}
	
		public string ToEnglish () {
			//doh. since i allow piecemeal input now, i need to kill this requirement.
			//note that this opens up the possibility of less-friendly exceptions than the one below
			if (!hasBeenParsed) {
				//return "Please parse the string first.";
			}
		
			StringBuilder sb = new StringBuilder();
		
			sb.Append("Repeat ");
			sb.Append(timeUnit.ToString());
		
			sb.Append(" every ");
			if (timeUnit == TimeUnit.Yearly) {
				sb.Append(EventRepeatParser.GetMonthName(everyNumber));
				//don't show this for theNumber events
				if (theNumber == 0) {
					sb.Append(" ");
					sb.Append(dayNumber);
					sb.Append(GetNumericSuffix(dayNumber));
				}
			} else {
				//0 here means repeat every occurence, such as every day
				if (everyNumber > 1) {
					sb.Append(everyNumber);
				}
				sb.Append(timeUnit == TimeUnit.Daily ? " day" : "");
				sb.Append(timeUnit == TimeUnit.Weekly ? " week" : "");
				sb.Append(timeUnit == TimeUnit.Monthly ? " month" : "");
				sb.Append(timeUnit == TimeUnit.Yearly ? " year" : "");
				if (everyNumber > 1) {
					sb.Append("s");
				}
			}
		
			if (dayArray.Length > 0) {
				sb.Append(" on ");
				if (theNumber > 0) {
					sb.Append("the ");
					sb.Append(theNumber);
					sb.Append(GetNumericSuffix(theNumber));
					sb.Append(" ");
				}
				string seperator = ", ";
				string doubleSeperator = " and ";
			
				if (dayArray.Length == 2) {
					seperator = doubleSeperator;
				}
			
				for (int i=0; i< dayArray.Length; i++) {
					sb.Append(dayArray[i]);
					if (i < dayArray.Length-1) {
						if (dayArray.Length > 2 && i == dayArray.Length-2) {
							sb.Append(doubleSeperator);
						} else {
							sb.Append(seperator);
						}
					}
				}
			}
		
			//monthly, though might be other cases. testing.
			if (dayNumber > 0 && timeUnit != TimeUnit.Yearly) {
				sb.Append(" on the ");
				sb.Append(dayNumber);
				sb.Append(GetNumericSuffix(dayNumber));
			}
		
			sb.Append(". ");
		
			sb.Append("Repeat Start Date = ");
			sb.Append(startDt.ToShortDateString());
			sb.Append(". ");
		
			if (endDt != null) {
				sb.Append("Repeat End Date = ");
				sb.Append(endDt.Value.ToShortDateString());
				sb.Append(". ");
			}
			if (occurNumber > 0) {
				sb.Append("Occurs ");
				sb.Append(occurNumber);
				sb.Append(" Time");
				if (occurNumber > 1) {
					sb.Append("s");
				}
				sb.Append(".");
			}
		
			//TODO: verify this is really true
			//i'm making the assumption here as i add this quite some time after its initial creation
			//that if we have gotten this far, it has been successfully parsed.
			hasBeenParsed = true;
		
			return sb.ToString();
		}
	
		//TODO: make a method to take the input parts and create the string we spent all this time parsing
		//this would be for dumping into the database
		public string ToDataString() {
			StringBuilder sb = new StringBuilder();
		
			sb.Append("repeat ");
			sb.Append(timeUnit.ToString().ToLower());
			sb.Append(" every(");
			sb.Append(everyNumber);
			sb.Append(")");
			if (theNumber > 0) {
				sb.Append(" the(");
				sb.Append(theNumber);
				sb.Append(")");
			} 
			if (dayNumber > 0) {
				sb.Append(" day(");
				sb.Append(dayNumber);
				sb.Append(")");
			}
			if (dayArray.Length > 0) {
				sb.Append("[");
				for (int i=0; i<dayArray.Length; i++) {
					sb.Append(dayArray[i]);
					if (i < dayArray.Length-1) {
						sb.Append(",");
					}
				}
				sb.Append("]");
			}
		
			sb.Append(" ");
		
			//date ranges
			sb.Append("{start(");
			sb.Append(startDt.Year);
			sb.Append("-");
			sb.Append(startDt.Month);
			sb.Append("-");
			sb.Append(startDt.Day);
			sb.Append(") ");
		
			if (occurNumber > 0) {
				sb.Append("occur(");
				sb.Append(occurNumber);
				sb.Append(")");
			}
		
			if (endDt != null) {
				sb.Append("end(");
				sb.Append(endDt.Value.Year);
				sb.Append("-");
				sb.Append(endDt.Value.Month);
				sb.Append("-");
				sb.Append(endDt.Value.Day);
				sb.Append(")");
			}
		
			sb.Append("}");
		
			return sb.ToString();
		}
	
		//based on the received criteria, generate a bunch of dates
		public ArrayList GenerateDateList () {
			ArrayList dates = new ArrayList();
			if (!hasBeenParsed) {
				throw new Exception("no information provided yet.");
			}
		
			//let's do the easy stuff first
			if (timeUnit == TimeUnit.Daily) {
				DateTime newDate = startDt;
				int dayCycle = this.everyNumber;
				if (dayCycle == 0) { dayCycle = 1; }
			
				if (endDt != null) {
					while (newDate <= endDt.Value) {
						dates.Add(newDate);
						newDate = newDate.AddDays(dayCycle);
					}
				} else {
					int count = 0;
					int maxCount = this.occurNumber;
					if (maxCount < 1) {
						maxCount = 1;
					}
					if (maxCount > 100) {
						maxCount = 100;
					}
					while (count < maxCount) {
						dates.Add(newDate);
						newDate = newDate.AddDays(dayCycle);
						count++;
					}
				}
			}
		
			if (timeUnit == TimeUnit.Weekly) {
				DateTime newDate = startDt;
				int weekCycle = this.everyNumber;
				if (weekCycle == 0) { weekCycle = 1; }
				//i can add days and add months, but not weeks.
				//so multiply this by 7
				weekCycle = weekCycle*7;
			
				//hrmmmmm
				//"Repeat Weekly every 2 weeks on thursday and saturday"
				//i need to figure out week boundaries and skip weeks
				//or...
				//figure out a count for skipping a day name a number of times based on the query
				/*	everyNumber-1 do a skip
					so if a thrusday every 3 weeks, skip 2 thursdays
				*/
				//set up counters
				//Hashtable dayCounter = new Hashtable();
				System.Collections.Generic.Dictionary<string, int> dayCounter =  new System.Collections.Generic.Dictionary<string, int>();
				//
				int skipNumber = this.everyNumber -1;
				if (skipNumber < 0) { skipNumber = 0; }
				int skipCounter = 0;
			
				//load the dayCcounter
				foreach (string day in DayArray) {
					if (!dayCounter.ContainsKey(day)) {
						dayCounter.Add(day, 0);
					}
				}
			
				if (endDt != null) {
					while (newDate <= endDt.Value) {
						string dayOfWeek = newDate.DayOfWeek.ToString().ToLower();
						if (dayCounter.ContainsKey(dayOfWeek)) {
							if (dayCounter[dayOfWeek] == 0) {
								dates.Add(newDate);
								dayCounter[dayOfWeek] = dayCounter[dayOfWeek]+1;
							} else {
								if (dayCounter[dayOfWeek] == skipNumber) {
									dayCounter[dayOfWeek] = 0;
								} else {
									dates.Add(newDate);
									dayCounter[dayOfWeek] = dayCounter[dayOfWeek]+1;
								}
							}
						}
					
						//increment
						newDate = newDate.AddDays(1);
					}
				} else {
					int maxCount = this.occurNumber;
					if (maxCount < 1) {
						maxCount = 1;
					}
					if (maxCount > 100) {
						maxCount = 100;
					}
				
					DateTime start = this.startDt;
				
					foreach (string day in DayArray) {
						string dayOfWeek = start.DayOfWeek.ToString().ToLower();
						while (dayCounter[day] < maxCount) {
							if (skipCounter == 0) {
								if (skipNumber > 0) { 
									skipCounter++;
								}
							
								dayOfWeek = start.DayOfWeek.ToString().ToLower();
								if (dayCounter.ContainsKey(dayOfWeek)) {
									//don't add the current date
									//if (start > startDt) {
										dates.Add(start);
									//}
									dayCounter[day] = dayCounter[day]+1;
								}
								start = start.AddDays(1);
							} else {
								if (skipCounter == skipNumber) {
									skipCounter = 0;
								} else {
									skipCounter++;
								}
								//scoot the time up one week
								start = start.AddDays(1);
							}
						}
					}	
				}
			}
		
			if (timeUnit == TimeUnit.Monthly) {
				/*
				"repeat monthly every 2nd month on the 12th day..." note: start(end|occur) syntax the same from now on.
				repeat monthly every(2) day(12) {}
			
				"repeat monthly every 2nd month on the 2nd Thursday..."
				repeat monthly every(2) the(2)[Thursday] {}
			
				//this is causing an explosion
				it's saying that the start date is 12/3/9999 12:00:00 AM, and adding a month to it's not a representable date.
			
				repeat monthly every(1) day(3) {start(2006-12-1) occur(10)}
			
				2008 12 22
				i ran into a bug
				i created monthly repeater starting on the first day of every month
				starting date: 2008 12 22
				it should have made the first date 2009 01 01, but it did 2008 instead
			
				*/
			
				//figure out the first date this can start.
				//get the current month, then check conditions.
				DateTime start = startDt;
				int skipNumber = everyNumber -1;
				if (skipNumber < 0) { skipNumber = 0; }
				int skipCounter = 0;
			
				if (this.dayNumber > 0) {
					if (start.Day != dayNumber) {
						//the given start date is not really the beginning of the occurence. 
						//look for it movign forward
						if (start.Day < dayNumber) {
							//look in current month
							//ugh. we can quickly run into end-of month scenarios
						
							//do a number diff. add the days
							start = start.AddDays(dayNumber - start.Day);
						
							//if this has kicked it to the next month, pull it back
							if (start.Month > startDt.Month) {
								start = new DateTime(start.Year, start.AddMonths(-1).Month, DateTime.DaysInMonth(start.Year, start.AddMonths(-1).Month));
							}
							//dates.Add(start);
						} else {
							//look in next month
							//start = new DateTime(start.Year, start.AddMonths(1).Month, dayNumber);
							start = new DateTime(start.AddMonths(1).Year, start.AddMonths(1).Month, dayNumber);
						
							//dates.Add(start);
						}
					}
				
					//occurence count, or end date...
					//both of these are gonna be a little different than day/week finders
					//i need to do some different cleaning of the dates
					int monthIncrement = 0;
					DateTime sourceDate = start;
					if (endDt != null) {
						//int sourceStartDay = sourceDate.Day;
						while (start <= endDt.Value) {
							//System.Web.HttpContext.Current.Response.Write("<div>" + skipNumber.ToString() + " : " + skipCounter.ToString() + " - " + start.ToShortDateString() + " - " + endDt.Value.ToShortDateString() + "</div>");
							if (skipCounter == 0 || skipNumber == 0) {
								skipCounter++;
								if (start.Day < this.dayNumber) {
									//some sanitation. make sure the day hasn't been pulled back in the case of dates near or at the end of a month
									int lastDay = DateTime.DaysInMonth(start.Year, start.Month);
									if (lastDay < this.dayNumber) {
										start = new DateTime(start.Year, start.Month, lastDay);
									} else {
										start = new DateTime(start.Year, start.Month, this.dayNumber);
									}
								}
								dates.Add(start);
							} else {
								if (skipCounter == skipNumber) {
									skipCounter = 0;
								} else {
									skipCounter++;
								}
							}
							monthIncrement++;
							//start = start.AddMonths(monthIncrement);
							start = start.AddMonths(1);
							//System.Web.HttpContext.Current.Response.Write("<div>" + start.ToShortDateString() + "</div>");
						}
					
					} else {
						//Occurence limiter section
					
						//reset this. just in case.
						skipCounter = 0;
					
						int currentCount = 0;
						int maxCount = this.occurNumber;
						if (maxCount < 1) {
							maxCount = 1;
						}
						if (maxCount > 100) {
							maxCount = 100;
						}
					
						/*
						if (start.Year == 9999) {
							throw new Exception("Start Date is not representable.");
						}
						*/
					
						while (currentCount < maxCount) {
							/*
							if (start.Year == 9999) {
								throw new Exception("Start Date is not representable. currentCount '" + currentCount.ToString() + "', skipNumber '" + skipNumber.ToString() + "'");
							}
							*/
							if (skipCounter == 0) {
								if (skipNumber > 0) { //this was causing a nearly infinte loop for things occuring every month
									skipCounter++;
								}
								if (start.Day < this.dayNumber) {
									//some sanitation. make sure the day hasn't been 
									//pulled back in the case of dates near or at the end of a month
									int lastDay = DateTime.DaysInMonth(start.Year, start.Month);
									if (lastDay < this.dayNumber) {
										start = new DateTime(start.Year, start.Month, lastDay);
										//throw new Exception("Start Date modified. currentCount '" + currentCount.ToString() + "'. new start '" + start.ToString() + "'");
									} else {
										start = new DateTime(start.Year, start.Month, this.dayNumber);
										//throw new Exception("Start Date modified. currentCount '" + currentCount.ToString() + "'. new start '" + start.ToString() + "'");
									}
								}
								dates.Add(start);
								currentCount++;
							} else {
								if (skipCounter == skipNumber) {
									skipCounter = 0;
								} else {
									skipCounter++;
								}
							}
							monthIncrement++; //wtf do i even use this?
						
							start = start.AddMonths(1);
						}
					}
				}
			
				if (this.theNumber > 0) {
					//"repeat monthly every 2nd month on the 2nd Thursday..."
				
					//rule: there can only be one named day defined.
					//so, knowing this, i will start on the 1st of each month, then iterate up by days until i reach the proper counted named day
				
					//set up counters
					//Hashtable dayCounter = new Hashtable();
					string namedDay = DayArray[0];
					int namedDayCounter = 1;
				
					//System.Web.HttpContext.Current.Response.Write("<div>named day  = " + namedDay + "</div>");
				
					//start on the first of this current month to see if it has an applicable entry
					start = new DateTime(start.Year, start.Month, 1);
				
					if (endDt != null) {
						while (start <= endDt.Value) {
							//System.Web.HttpContext.Current.Response.Write("<div>date = " + start.ToShortDateString() + ", namedDayCounter = " + namedDayCounter.ToString() + "</div>");
							//System.Web.HttpContext.Current.Response.Write("<div>date eval  = " + start.ToShortDateString() + " - " + start.DayOfWeek.ToString() + "</div>");
							if (skipCounter == 0 || this.everyNumber == 1) { //this is the month skipper
								string dayOfWeek = start.DayOfWeek.ToString().ToLower();
								if (namedDay == dayOfWeek) {
									if (namedDayCounter == this.theNumber) {
										if (start >= startDt) {
											dates.Add(start);
											skipCounter++;
										}
									
										namedDayCounter = 1; //reset
										//scoot to next month
										//i would just make it next month with day 1, but if it's in december.. i need the year to scoot too
										start = start.AddMonths(1);
										start = new DateTime(start.Year, start.Month, 1);
									} else {
										namedDayCounter++;
										start = start.AddDays(1);
									}
								} else {
									start = start.AddDays(1);
								}
							} else {
								if (skipCounter == skipNumber) {
									skipCounter = 0;
								} else {
									skipCounter++;
								}
								//scoot to the first of next month
								start = start.AddMonths(1);
								start = new DateTime(start.Year, start.Month, 1);
								namedDayCounter = 1; //reset
							}
						}
					} else {
					
						int maxCount = this.occurNumber;
						int currentCount = 0;
						if (maxCount < 1) {
							maxCount = 1;
						}
						if (maxCount > 100) {
							maxCount = 100;
						}
					
						while (currentCount < maxCount) {
							//System.Web.HttpContext.Current.Response.Write("<div>date eval  = " + start.ToShortDateString() + " - " + start.DayOfWeek.ToString() + "</div>");
							if (skipCounter == 0 || this.everyNumber == 1) { //this is the month skipper
								string dayOfWeek = start.DayOfWeek.ToString().ToLower();
								if (namedDay == dayOfWeek) {
									if (namedDayCounter == this.theNumber) {
										if (start >= startDt) {
											dates.Add(start);
											skipCounter++;
											currentCount++;
										}
										namedDayCounter = 1; //reset
										//scoot to next month
										//i would just make it next month with day 1, but if it's in december.. i need the year to scoot too
										start = start.AddMonths(1);
										start = new DateTime(start.Year, start.Month, 1);
									} else {
										namedDayCounter++;
										start = start.AddDays(1);
									}
								} else {
									start = start.AddDays(1);
								}
							} else {
								if (skipCounter == skipNumber) {
									skipCounter = 0;
								} else {
									skipCounter++;
								}
								//scoot to the first of next month
								start = start.AddMonths(1);
								start = new DateTime(start.Year, start.Month, 1);
							}
						}
					}
				}
			}
		
			if (timeUnit == TimeUnit.Yearly) {
				/*
				"repeat yearly every October 12..."
				repeat yearly every(10) day(12) {}
			
				"repeat yearly every second Thursday of October"
				repeat yearly every(10) the(2)[Thursday] {}
				*/
				DateTime start = this.startDt;
				int month = this.everyNumber;
				int skipNumber = theNumber-1;
				if (skipNumber < 0) { skipNumber = 0; }
				int skipCounter = 0;
			
				if (this.dayNumber > 0) {
					int day = this.dayNumber;
				
					//normailize the start date to the intended repeat sequence
					DateTime realStart = new DateTime(start.Year, month, (day > DateTime.DaysInMonth(start.Year, month) ? DateTime.DaysInMonth(start.Year, month) : day));
					if (start > realStart) {
						start = realStart.AddYears(1);
					}
					if (start < realStart) {
						start = realStart;
					}
				
					if (this.endDt != null) {
						while (start <= this.endDt.Value) {
							dates.Add(start);
							start = start.AddYears(1);
							//do some sanitation. since months like february have variable length year to year
							if (start.Day < day) {
								start = new DateTime(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month));
							}
						}
					} else {
						int maxCount = this.occurNumber;
						int currentCount = 0;
						if (maxCount < 1) {
							maxCount = 1;
						}
						if (maxCount > 100) {
							maxCount = 100;
						}
					
						while (currentCount < maxCount) {
							dates.Add(start);
							start = start.AddYears(1);
							//do some sanitation. since months like february have variable length year to year
							if (start.Day < day) {
								start = new DateTime(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month));
							}
						
							currentCount++;
						}
					}
				}
			
				if (this.theNumber > 0) {
					//the date normalization is a bit different
					/*	if the year/month is equal to the intended repeat, go to the 1st of the month, iterate forward. see if 
						the startDt is before or after.
						if the startDt is before, go with the new date
						if the startDt is after, it's time to scoot to next year to try
					*/
					/*	if the year/month is greater than the intended repeat, go to next year and start at the first day of that month
						then iterate forward to get the correct day of the week on the correct counter
					*/
					/*	if the year/month is less than the intended repeat (easiest scenario),
						go to the year/month intended date, on the 1st day of that month
						then do the look for the correct dayOfWeek counter
					*/
				
					string namedDay = DayArray[0];
					int namedDayCounter = 1;
				
					if (start.Month == month) {
						start = new DateTime(start.Year, start.Month, 1);
					}
					if (start.Month > month) {
						start = new DateTime(start.AddYears(1).Year, month, 1);
					}
					if (start.Month < month) {
						start = new DateTime(start.Year, month, 1);
					}
				
				
				
					if (endDt != null) {
						while (start <= endDt.Value) {
							//System.Web.HttpContext.Current.Response.Write("<div>date eval  = " + start.ToShortDateString() + " - " + start.DayOfWeek.ToString() + "</div>");
							string dayOfWeek = start.DayOfWeek.ToString().ToLower();
							if (namedDay == dayOfWeek && start > startDt) {
								if (namedDayCounter == this.theNumber) {
									dates.Add(start);
									namedDayCounter = 1; //reset
									//scoot to next month
									//i would just make it next month with day 1, but if it's in december.. i need the year to scoot too
									start = start.AddYears(1);
									start = new DateTime(start.Year, start.Month, 1);
									skipCounter++;
								} else {
									namedDayCounter++;
									start = start.AddDays(1);
								}
							} else {
								start = start.AddDays(1);
							}
						}
					} else {
						int maxCount = this.occurNumber;
						int currentCount = 0;
						if (maxCount < 1) {
							maxCount = 1;
						}
						if (maxCount > 100) {
							maxCount = 100;
						}
					
						while (currentCount < maxCount) {
							//System.Web.HttpContext.Current.Response.Write("<div>date eval  = " + start.ToShortDateString() + " - " + start.DayOfWeek.ToString() + "</div>");
							string dayOfWeek = start.DayOfWeek.ToString().ToLower();
							if (namedDay == dayOfWeek && start > startDt && start.Month == month) {
								if (namedDayCounter == this.theNumber) {
									dates.Add(start);
									namedDayCounter = 1; //reset
									//scoot to next year
									//i would just make it next month with day 1, but if it's in december.. i need the year to scoot too
									start = start.AddYears(1);
									start = new DateTime(start.Year, start.Month, 1);
									skipCounter++;
									currentCount++;
								} else {
									namedDayCounter++;
									start = start.AddDays(1);
								}
							} else {
								start = start.AddDays(1);
							}
						}
					}
				}
			}
		
			return dates;
		}
	
		/*
		private DateTime sanitizeMonthlyDate(
			DateTime inputDate,
			int intendedMonth
		) {
			if (inputDate.Month > intendedMonth) {
				inputDate = new DateTime(inputDate.Year, inputDate.AddMonths(-1).Month, DateTime.DaysInMonth(inputDate.Year, inputDate.AddMonths(-1).Month));
			}
			return inputDate;
		}
		*/
	
		public static string GetMonthName(int num) {
			if (num < 1 || num > 12) {
				throw new ArgumentOutOfRangeException("Error, months only exist from 1 to 12. You supplied '" + num.ToString() + "'");
			}
			if (monthListByNumber == null) {
				monthListByNumber = new scg.Dictionary<int, string>(12);
				monthListByNumber.Add(1, "January");
				monthListByNumber.Add(2, "February");
				monthListByNumber.Add(3, "March");
				monthListByNumber.Add(4, "April");
				monthListByNumber.Add(5, "May");
				monthListByNumber.Add(6, "June");
				monthListByNumber.Add(7, "July");
				monthListByNumber.Add(8, "August");
				monthListByNumber.Add(9, "September");
				monthListByNumber.Add(10, "October");
				monthListByNumber.Add(11, "November");
				monthListByNumber.Add(12, "December");
			}
			return monthListByNumber[num].ToString();
		}
	
		public static string GetNumericSuffix(int num) {
			if (num == 0) {
				return "";
			}
		
			string urf =  num.ToString();
		
			if (urf.EndsWith("1")) {
				return "st";
			}
			if (urf.EndsWith("2")) {
				return "nd";
			}
			if (urf.EndsWith("3")) {
				return "rd";
			}
		
			return "th";
		}
	
		public string Input {
			get { return input; }
			set { input = value; }
		}
	
		public TimeUnit TimeUnit {
			get { return timeUnit; }
			set { timeUnit = value; }
		}
	
		//the 'every' field
		public int EveryNumber {
			get { return everyNumber; }
			set { everyNumber = value; }
		}
	
		public int DayNumber {
			get { return dayNumber; }
			set { dayNumber = value; }
		}
	
		public int TheNumber {
			get { return theNumber; }
			set { theNumber = value; }
		}
	
		public string[] DayArray {
			get { return dayArray; }
			set { dayArray = value; }
		}
	
		public DateTime StartDt {
			get { return startDt; }
			set { startDt = value; }
		}
	
		public DateTime? EndDt {
			get { return endDt; }
			set { endDt = value; }
		}
	
		public int OccurNumber {
			get { return occurNumber; }
			set { occurNumber = value; }
		}
	
		public bool HasBeenParsed {
			get { return hasBeenParsed; }
		}
	
		private string input;
		private TimeUnit timeUnit;
		private int everyNumber;
		private int dayNumber;
		private int theNumber;
		private string[] dayArray;
		private DateTime startDt;
		private DateTime? endDt;
		private int occurNumber;
		private bool hasBeenParsed;
		private static scg.Dictionary<int, string> monthListByNumber;
	}

	public enum TimeUnit
	{
		Unknown = 0,
		Daily = 1,
		Weekly = 2,
		Monthly = 4,
		Yearly = 8
	}
}
