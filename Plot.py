# -*- coding: utf-8 -*-
"""
Created on Mon Feb 15 19:31:54 2016

@author: Libo
"""

import json

from dateutil.parser import parse
import numpy as np
import matplotlib.cm as cm
import matplotlib.pyplot as plt

np.random.seed(seed=756)

regionDict = {}
dateDict = {}
noData = []

with open ("regionDict.json", "r", encoding='utf-8-sig') as datafile:
    regionDict = json.load(datafile)
    
with open ("dateDict.json", "r", encoding='utf-8-sig') as datafile:
    dateDict = json.load(datafile)
    
with open ("noData.json", "r", encoding='utf-8-sig') as datafile:
    noData = json.load(datafile)

all_regions = ["americas", "europe", "se_asia", "china"]


region_averages = {i : [] for i in all_regions}
region_min = {i : [] for i in all_regions}
region_std_dev = {i : [] for i in all_regions}


for region, data in regionDict.items():
    for date, player_data in data.items():
        np_data = np.array([player['mmr'] for player in player_data ])
        average = np_data.mean()
        min_value = np_data.min()
        st_dev = np_data.std()

        datetimeDate = parse(date)
        region_avg_data = (datetimeDate, average)
        region_averages[region].append(region_avg_data)
        
        region_min_data = (datetimeDate, min_value)
        region_min[region].append(region_min_data)
        
        region_std_dev_data = (datetimeDate, st_dev)
        region_std_dev[region].append(region_std_dev_data)

colors = cm.rainbow(np.linspace(0, 1, 12))
np.random.shuffle(colors)
plt.figure(figsize=(15, 15)) # This increases resolution

for index, region in enumerate(all_regions):
    x_val = [x[0] for x in region_averages[region]]
    y_val = [x[1] for x in region_averages[region]]
    plt.scatter(x_val, y_val, label=region, color=colors[index])
    
    x_val = [x[0] for x in region_min[region]]
    y_val = [x[1] for x in region_min[region]]
    plt.scatter(x_val, y_val, label=(region+"min"), color=colors[index + 4], marker='x', s=1.25)
    
    

plt.legend()

plt.show()

plt.figure(figsize=(15, 15)) # This increases resolution
for index, region in enumerate(all_regions):
    x_val = [x[0] for x in region_std_dev[region]]
    y_val = [x[1] for x in region_std_dev[region]]
    plt.scatter(x_val, y_val, label=(region+"std_dev"), color=colors[index + 8])

plt.legend()

plt.show()
