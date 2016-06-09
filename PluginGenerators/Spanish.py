# -*- coding: utf-8 -*-
from json import *
from collections import OrderedDict

patterns = OrderedDict()

patterns["Patterns"] = {
    0: {
        "ar$": ["o", "as", "a", "amos", "áis", "an"] # present for -ar
        }
    }
patterns["Prefixes"] = []


with open("Spanish.json", mode='w', encoding='utf-8') as file:
            file.write(dumps(patterns))
