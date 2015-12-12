# -*- coding: utf-8 -*-
from json import *
from collections import OrderedDict

patterns = OrderedDict()
patterns["Patterns"] = {
    0: {
			"[^ןםךףץה]$": ["$&"],
		    "ם$": ["$&", 'מ'],  # use pattern unmodified on the next
            "ן$": ["$&", 'נ'],   # level
            "ך$": ["$&", 'כ'],
            "ף$": ["$&", 'פ'],
			"ץ$": ["$&", 'צ'],
			"ה$": ["$&", '']  # strip to masculine form
	},
    1: {"ה$":  # Special cases for hey
         ["תך", "תו", "תה", "ית", "יתי", "תכם", "תכן", "תנו", "יתם", "יתן"],
        "ייה$": ["יות"],
        "ות$": ["ויות"],
        "^.+[^ןםךףץה]$":  # ^ start of the string, .+ any # of chars, no sofits on the end
         ["$&ים","$&ת","$&ות","$&ה","$&תי",
          "$&נו", "$&ו", "$&תם", "$&תן", "$&י",
          "$&ך", "$&כם", "$&כן", "$&ם", "$&ן",
          "$&יי", "$&יך", "$&ייך", "$&יו",
          "$&יה", "$&ינו", "$&יכם", "$&יכן",
          "$&יהם", "$&יהן", "$&ותי", "$&ותיך",
          "$&ותייך", "$&ותיו", "$&ותיה", "$&ותינו",
          "$&ותיכם", "$&ותיכן", "$&ותיהם", "$&ותיהן", "$&ית"],
        "י(?<beg>.+[^מנפכצ]$)":
         ["א${beg}", "נ${beg}", "ת${beg}"],  # future
        "י(?<beg>.{2})ו(?<end>[^ןםךףץה]$)":  # future ephol i(?<beginning>.+)o(?<ending>[^h])
         ["ת${beg}${end}י", "ת${beg}${end}ו", "י${beg}${end}ו"],
        "י(?<beg>.*[^ו][^ןםךףץה]$)":  # future ephal i(?<beginning>.+[^o][^h]$)
         ["ת${beg}י", "ת${beg}ו", "י${beg}ו"],
        "י(?<beg>.ו[^ןםךףץה]$)":  # future aynvav
         ["ת${beg}י", "ת${beg}ו", "י${beg}ו"],
        "י(?<beg>.+)ה$":
         ["ת${beg}י", "ת${beg}ו", "י${beg}ו"],
        "(?<beg>^ה.+)י(?<end>[^ןםךףץה]$)":
         ["${beg}${end}ת", "${beg}${end}תי", "${beg}${end}נו",
          "${beg}${end}תם", "${beg}${end}תן"],  # hifil (?<beginning>^h.+)i(?<ending>[^a])
        "(?<beg>^ה.+)י(?<end>נ$)": ["${beg}${end}ו"]  # hifil double nun special case
    }
}

patterns["Prefixes"] = ["ה", "ל", "ב", "לב", "מ", "מה", "כ", "כה", "כש",
                      "כשה", "ש", "שה", "ו", "וה", "וב", "ול", "ולב", "ומ",
                      "ומה", "וכ", "וכה", "וכש", "וכשה", "וש", "ושה", "ת'",
                      "שב", "ושב", "כשב", "שכש", "שכשה", "של"
                      ]

with open("Hebrew.json", mode='w', encoding='utf-8') as file:
    file.write(dumps(patterns))