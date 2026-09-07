"""Authored Atlantis interiors: furnished rooms, marine life and ruin, drawn from
Atlantis's own decorated rooms plus the Tower of Brass, Undersea Castle, Azure
Castle, Caverns of Poseidon and Dungeon Ankh. Writes the manifest only; never
touches MULs or saves."""
from pathlib import Path
from collections import defaultdict, Counter, deque
import struct
WORLD=Path(__file__).resolve().parents[2]
FILES=WORLD/'Data/Files'; FLOOR_Z=-127
SECTIONS={'north':(4318,2519,4472,2584),'central':(4320,2584,4380,2636),'east':(4380,2584,4472,2636),'south':(4380,2636,4472,2684)}

# Hues observed in Atlantis's existing statics (see Documentation/Atlantis/style-guide.md).
STONE=2722; OLIVE=2001; WOOD=2739; MARBLE=2826; TEAL=2748; PED=2747; SHELF=2764; ITEM=2767
BONE=2765; ARMOR=2831; CRATE=2859; SEA=2879; CRYS=2319; PLANT=2977; MOSS=2005; RUG=2746
DARK=2944; ROCK=2827; VERDIGRIS=2897; GREENWOOD=2967; SETTING=2766

def load_statics():
    idx=(FILES/'staidx1.mul').read_bytes();data=(FILES/'statics1.mul').read_bytes();cells=defaultdict(list)
    for bx in range(515,568):
        for by in range(303,346):
            off,size,_=struct.unpack_from('<iii',idx,(bx*512+by)*12)
            if off<0 or size<=0:continue
            for i,x,y,z,h in struct.iter_unpack('<HBBbH',data[off:off+size]):
                if -128<=z<-87:cells[bx*8+x,by*8+y].append((i,z,h))
    return cells
def is_floor(s):return 0x519<=s[0]<=0x524 and s[1]==FLOOR_Z
def is_wall(s):return 0xC7<=s[0]<=0xE6
def connected_components(points):
    todo=set(points);result=[]
    while todo:
        start=todo.pop();queue=deque([start]);component={start}
        while queue:
            x,y=queue.popleft()
            for p in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if p in todo:todo.remove(p);component.add(p);queue.append(p)
        result.append(component)
    return result

TILEDATA=(FILES/'tiledata.mul').read_bytes()
def metadata(i):
    off=493568+(i//32)*(4+32*41)+4+(i%32)*41
    return struct.unpack_from('<Q',TILEDATA,off)[0],TILEDATA[off+20]
def height(i):return metadata(i)[1]

# Existing Atlantis pools, copied tile for tile (4288,2602 and 4362,2593).
POOL_SMALL=[(1,0,0x5C81),(2,0,0x5C49),(3,0,0x5C82),(0,1,0x5C81),(1,1,0x5C7F),(3,1,0x5C7C),(4,1,0x5C82),(0,2,0x5C4C),(4,2,0x5C7C),(5,2,0x5C82),(0,3,0x5C80),(1,3,0x5C7E),(5,3,0x5C47),(1,4,0x5C80),(2,4,0x5C4B),(3,4,0x5C7E),(5,4,0x5C46),(3,5,0x5C80),(4,5,0x5C4A),(5,5,0x5C83)]
POOL_LARGE=[(2,0,0x5C81),(3,0,0x5C48),(4,0,0x5C49),(5,0,0x5C82),(0,1,0x5C81),(1,1,0x5C49),(2,1,0x5C7F),(3,1,0x5D29),(4,1,0x5C16),(5,1,0x5C7C),(6,1,0x5C82),(0,2,0x5C4C),(1,2,0x5D1F),(2,2,0x5D29),(3,2,0x5C17),(4,2,0x5D1F),(5,2,0x5D1F),(6,2,0x5C46),(0,3,0x5C80),(1,3,0x5C7E),(2,3,0x5D1F),(3,3,0x5D29),(4,3,0x5D1F),(5,3,0x5C17),(6,3,0x5C47),(1,4,0x5C4D),(2,4,0x5D1F),(3,4,0x5D1F),(4,4,0x5D29),(5,4,0x5C7D),(6,4,0x5C83),(1,5,0x5C80),(2,5,0x5C4B),(3,5,0x5C7E),(4,5,0x5C16),(5,5,0x5C47),(3,6,0x5C80),(4,6,0x5C4A),(5,6,0x5C83)]
JELLY=(0x6210,0x6211,0x6212,0x6213,0x6214,0x6215)
FISH_HUES=(0,2817,2814,2966,2929)
CRATES=(0x4F86,0x4F87,0x4F88,0x4F8A,0x4F8B,0x4F8D,0x4F8F,0x4F90,0x4F92,0x4F94,0x4F96,0x4F98)

class Room:
    def __init__(self,district,name):self.district=district;self.key=district+'_v2_'+name;self.items=[]
    def add(self,i,h,x,y,z=-127,mode='floor'):self.items.append((i,h,x,y,z,mode))
    def over(self,i,h,x,y):self.add(i,h,x,y,-127,'overlay')
    def surf(self,i,h,x,y,z):self.add(i,h,x,y,z,'surface')
    def north(self,i,h,x,y,z=-127):self.add(i,h,x,y,z,'north')
    def west(self,i,h,x,y,z=-127):self.add(i,h,x,y,z,'west')
    # --- light ---
    def lamp(self,x,y,west=False,sconce=False):
        i=(0x6384 if west else 0x6389) if sconce else (0x6380 if west else 0x6385)
        (self.west if west else self.north)(i,CRYS,x,y,-110)
    def brazier(self,x,y,small=False):self.add(0xE31 if small else 0x19AA,CRYS,x,y)
    def candelabra(self,x,y,alt=False):self.add(0x52E if alt else 0x52D,ITEM,x,y)
    def lamppost(self,x,y,ornate=False):self.add(0x65E0 if ornate else 0xB20,ITEM,x,y)
    def lantern(self,x,y,west=False):(self.west if west else self.north)(0xA15,0,x,y,-114)
    # --- tables ---
    def table(self,x,y,length=3,kind='stone',west=False,contents=(),hue=None):
        """One-tile-wide table; kind in stone, marble, wood, log. Contents sit on top."""
        ends={'stone':((0x1202,0x1203,0x1201),(0x1205,0x1206,0x1204),STONE),
              'marble':((0x1DC2,0x1DC3,0x1DC1),(0x1DC5,0x1DC6,0x1DC4),MARBLE),
              'wood':((0xB6C,0xB6D,0xB6B),(0xB7F,0xB80,0xB7E),WOOD),
              'log':((0x11DD,0x11DE,0x11DC),(0x11DD,0x11DE,0x11DC),WOOD)}[kind]
        pieces=ends[0] if west else ends[1]
        for k in range(length):
            i=pieces[0] if k==0 else pieces[2] if k==length-1 else pieces[1]
            self.add(i,hue if hue is not None else ends[2],x if west else x+k,y+k if west else y)
        for i,k,*rest in contents:
            h=rest[0] if rest else ITEM
            self.surf(i,h,x if west else x+k,y+k if west else y,-121)
    def small_table(self,x,y,i=0xB34,contents=()):
        self.add(i,WOOD,x,y)
        for c,*rest in contents:self.surf(c,rest[0] if rest else ITEM,x,y,-127+height(i))
    def writing_table(self,x,y,contents=()):self.small_table(x,y,0xB4C,contents)
    def desk(self,x,y,west=False,scholar=True):
        # Atlantis 4296,2632: three-part blue stone desk, raised contents, chair.
        for k,i in enumerate((0x1202,0x1203,0x1201) if west else (0x1205,0x1206,0x1204)):
            xx=x if west else x+k;yy=y+k if west else y
            self.add(i,STONE,xx,yy)
            self.surf((0x1810,0x3B51,0x3031)[k] if scholar else (0x9AE,0x995,0x9A7)[k],ITEM,xx,yy,-121)
        self.add(0x121B if west else 0x121A,STONE,x+1,y+1)
    # --- seats ---
    def chair(self,x,y,facing='s',kind='stone',hue=None):
        ids={'stone':{'s':0x1218,'n':0x121A,'e':0x1219,'w':0x121B},'wood':{'s':0xB59,'n':0xB5B,'e':0xB5A,'w':0xB58}}[kind]
        self.add(ids[facing],hue if hue is not None else (STONE if kind=='stone' else WOOD),x,y)
    def bench(self,x,y):
        for k,i in enumerate((0x1DC8,0x1DC9,0x1DC7)):self.add(i,TEAL,x,y+k)
    def marble_bench(self,x,y,west=False):self.add(0x1DD1 if west else 0x1DD0,MARBLE,x,y)
    def stone_bench(self,x,y,alt=False):self.add(0x45C if alt else 0x45B,TEAL,x,y)
    def wood_bench(self,x,y,west=False):self.add(0xB2D if west else 0xB2C,WOOD,x,y)
    def throne(self,x,y,z=-127,mode='floor'):self.add(0x4023,STONE,x,y,z,mode)
    def golden_throne(self,x,y):self.add(0x19EE,MARBLE,x,y-1);self.add(0x19ED,MARBLE,x,y)
    # --- storage / cases ---
    def shelf(self,x,y,west=False,broken=False,vines=False):
        (self.west if west else self.north)(0x3CB1 if broken else 0x3CC1 if west else 0x3CC2,SHELF,x,y)
        if broken:self.surf(0xC30,ITEM,x,y,-115)
        if vines:self.over(0xC5E,PLANT,x,y)
    def bookcase(self,x,y,west=False,vines=True,dusty=False):
        i=(0xC14 if west else 0xC15) if dusty else (0xA97 if west else 0xA99)
        (self.west if west else self.north)(i,SHELF,x,y)
        if vines:self.over(0xC5E,PLANT,x,y)
    def crate(self,x,y,i=0x4F86,top=None):
        self.add(i,CRATE,x,y)
        if top:self.surf(top,ITEM,x,y,-121)
    def crates(self,x,y,seed=0):
        for k,(dx,dy) in enumerate(((0,0),(1,0),(0,1))):self.crate(x+dx,y+dy,CRATES[(seed+k)%len(CRATES)])
    def barrel(self,x,y,i=0xFAE):self.add(i,ITEM,x,y)
    def chest(self,x,y,i=0xE42,h=None):self.add(i,h if h is not None else GREENWOOD,x,y)
    def urn(self,x,y,i=0x1AFC):self.add(i,ITEM,x,y)
    def vase(self,x,y,i=0x4C2A):self.add(i,MARBLE,x,y)
    # --- statues and masonry ---
    def statue(self,x,y,i=0x4C33,h=MARBLE):
        self.add(0x750,PED,x,y);self.surf(i,h,x,y,-122)
    def column(self,x,y,broken=False):self.add((0x45B4,0x45B5)[(x+y)%2] if broken else 0x11B,TEAL,x,y)
    def dais(self,x,y,w,h,stairs='s'):
        for dx in range(w):
            for dy in range(h):self.add(0x71E,TEAL,x+dx,y+dy)
        if stairs=='s':
            for dx in range(w):self.add(0x736,TEAL,x+dx,y+h)
        elif stairs=='w':
            for dy in range(h):self.add(0x737,TEAL,x-1,y+dy)
    def on_dais(self,i,h,x,y):self.surf(i,h,x,y,-122)
    def ruin(self,x,y):
        self.add(0x45B4,TEAL,x,y);self.add(0x136D,ROCK,x+1,y+1)
        self.add(0xCA9,PLANT,x,y+2);self.add(0x624B,0,x+2,y+2)
    def rubble(self,x,y):
        self.add(0x136B,ROCK,x,y);self.add(0x136D,ROCK,x+1,y);self.over(0x30EA,0,x,y+1);self.add(0x278,ROCK,x+1,y+1)
    def sinkhole(self,x,y,i=0x30E7):self.over(i,0,x,y)
    def cracks(self,x,y):self.over(0x1B01+(x+y)%4,0,x,y)
    def altar_disc(self,x,y):
        for k,i in enumerate((0x120E,0x120F,0x1210,0x1215,0x1216,0x1211,0x1214,0x1213,0x1212)):self.over(i,TEAL,x+k%3,y+k//3)
    def fountain(self,x,y):
        for k,i in enumerate((0x90D,0x90E,0x90F,0x910)):self.add(i,TEAL,x+k%2,y+k//2)
    def sarcophagus(self,x,y):
        for k,i in enumerate((0x1CAD,0x1CAE,0x1CAF)):self.add(i,TEAL,x,y+k)
    def tomb(self,x,y):
        for k,i in enumerate((0x1C62,0x1C61,0x1C60)):self.add(i,TEAL,x+k,y)
    # --- floors ---
    def rug(self,x,y,w,h,holes=()):
        # Existing banquet-room carpet, preserving holes rather than filling every tile.
        # The torn patch is the cave-floor edge artwork used in Atlantis's carpet at 4299,2606.
        tear=(( (0xAC8,), (0x542,), (0x543,), (0xAC8,), (0xAC8,) ),
              ( (0x553,0x551), (), (), (0x552,), (0xAC8,) ),
              ( (0xAC8,), (0x553,), (), (), (0x552,0x56A) ),
              ( (0xAC8,), (0xAC8,), (0x54D,), (0x54F,), (0xAC8,) ))
        for dy in range(h):
            for dx in range(w):
                if (dx,dy) in holes:continue
                tx=dx-(w//2-2);ty=dy-(h//2-2)
                if w>=8 and h>=7 and 0<=tx<5 and 0<=ty<4:
                    for i in tear[ty][tx]:self.over(i,RUG,x+dx,y+dy)
                    continue
                i=0xACA if dx==dy==0 else 0xACC if dx==w-1 and dy==0 else 0xACB if dx==0 and dy==h-1 else 0xAC9 if dx==w-1 and dy==h-1 else 0xACE if dy==0 else 0xAD0 if dy==h-1 else 0xACD if dx==0 else 0xACF if dx==w-1 else 0xAC8
                self.over(i,RUG,x+dx,y+dy)
    def ornate(self,x,y,w,h,hue=SEA,holes=()):
        # Undersea Castle carpet family 0xAEE-0xAFA. Narrow runners use field tiles only,
        # so a carpet is never just its border pieces.
        for dy in range(h):
            for dx in range(w):
                if (dx,dy) in holes:continue
                i=0xAFA if w<3 or h<3 else 0xAEF if dx==dy==0 else 0xAF1 if dx==w-1 and dy==0 else 0xAF0 if dx==0 and dy==h-1 else 0xAEE if dx==w-1 and dy==h-1 else 0xAF3 if dy==0 else 0xAF5 if dy==h-1 else 0xAF2 if dx==0 else 0xAF4 if dx==w-1 else 0xAFA
                self.over(i,hue,x+dx,y+dy)
    def mosaic(self,x,y,w,h,hue=SEA,medallion=False,holes=()):
        # Azure Castle mosaic family 0x56B8-0x56E2.
        for dy in range(h):
            for dx in range(w):
                if (dx,dy) in holes:continue
                i=0x56C5 if w<3 or h<3 else 0x56C9 if dx==dy==0 else 0x56CB if dx==w-1 and dy==0 else 0x56CA if dx==0 and dy==h-1 else 0x56C8 if dx==w-1 and dy==h-1 else 0x56CD if dy==0 else 0x56CF if dy==h-1 else 0x56CC if dx==0 else 0x56CE if dx==w-1 else 0x56C5
                if medallion and dx==w//2 and dy==h//2:i=0x56D0
                self.over(i,hue,x+dx,y+dy)
    def paving(self,x,y,w,h,hue=STONE):
        for dy in range(h):
            for dx in range(w):self.over(0x917 if (dx+dy)%2 else 0x918,hue,x+dx,y+dy)
    def moss(self,x,y,pieces=((0,0,0x542),(1,0,0x543),(0,1,0x54D),(1,1,0x54F)),hue=MOSS):
        for dx,dy,i in pieces:self.over(i,hue,x+dx,y+dy)
    def deck(self,x,y,w,h):
        for dy in range(h):
            for dx in range(w):self.over(0x4AB,PED,x+dx,y+dy)
    # --- water and marine life ---
    def pool(self,x,y,large=False,life='jelly',jelly=0):
        """Existing Atlantis pool shapes. life: jelly, fish, lily, octopus, bones or None."""
        for dx,dy,i in (POOL_LARGE if large else POOL_SMALL):self.over(i,SEA if large else 0,x+dx,y+dy)
        if not life:return
        cx,cy=(x+3,y+3) if large else (x+2,y+3)
        self.over(0x79A7,0,cx+1,cy)
        if life=='jelly':
            self.add(JELLY[jelly%6],0,cx,cy-1);self.over(0xCA7 if not large else 0xCF2,PLANT,cx-1,cy-1);self.over(0x540D,FISH_HUES[jelly%5],cx-1,cy+1)
        elif life=='fish':
            self.over(0x540D,FISH_HUES[jelly%5],cx,cy-1);self.over(0x540D,FISH_HUES[(jelly+2)%5],cx+1,cy+1);self.over(0xDD8,0,cx-1,cy);self.over(0xDD6,0,cx,cy+1);self.over(0xDBA,PLANT,cx+1,cy-1)
        elif life=='lily':
            self.over(0xD0B,0,cx,cy-1);self.over(0xD08,0,cx+1,cy+1);self.over(0xD07,0,cx-1,cy);self.over(0xD09,0,cx,cy+1);self.over(0xCB6,PLANT,cx-1,cy-1)
        elif life=='octopus':
            self.over(0x52C9,0,cx,cy);self.over(0x52E1,0,cx-1,cy-1);self.over(0xDBB,PLANT,cx+1,cy-1);self.crab(cx-1,cy+1,0x531F)
        elif life=='bones':
            self.over(0xECF,BONE,cx,cy);self.over(0x1AE1,BONE,cx-1,cy-1);self.over(0xECB,BONE,cx+1,cy+1);self.over(0xDBA,PLANT,cx-1,cy+1)
    def mushrooms(self,x,y):self.over(0xD13,PLANT,x,y);self.over(0xD14,PLANT,x+1,y+1);self.over(0xD16,PLANT,x+1,y)
    def hay(self,x,y):self.over(0x1037,1881,x,y);self.over(0x1036,1881,x+1,y);self.over(0xF35,1881,x,y+1)
    def loom(self,x,y):self.add(0x105F,WOOD,x,y);self.add(0x1060,WOOD,x,y+1)
    def paving_ring(self,x,y,w,h,hue=STONE):
        for dx in range(w):self.over(0x917 if dx%2 else 0x918,hue,x+dx,y);self.over(0x918 if dx%2 else 0x917,hue,x+dx,y+h-1)
        for dy in range(1,h-1):self.over(0x917 if dy%2 else 0x918,hue,x,y+dy);self.over(0x918 if dy%2 else 0x917,hue,x+w-1,y+dy)
    def kelp(self,x,y,tall=True):self.add(0x6593 if tall else 0xDBA,PLANT,x,y) if tall else self.over(0xDBA,PLANT,x,y)
    def growth(self,x,y):
        for dx,dy,i in ((0,0,0xCB0),(1,0,0xCAD),(0,1,0xCEE),(1,1,0xCAC)):self.over(i,PLANT,x+dx,y+dy)
    def coral(self,x,y,i=0x6595):self.add(i,0,x,y)
    def crab(self,x,y,i=0x22A9):self.over(i,0,x,y)
    def fishes(self,x,y):self.over(0xDD8,0,x,y)
    def shells(self,x,y):self.add(0x77B5,0,x,y);self.add(0x77B7,0,x+1,y)
    def crystal(self,x,y,i=0x61DD):self.add(i,0,x,y)
    def glow(self,x,y):self.over(0x6764,CRYS,x,y)
    def bones(self,x,y,i=0xECA):self.over(i,BONE,x,y)
    def skeleton(self,x,y,i=0x1236,z=-127,mode='floor'):self.add(i,BONE,x,y,z,mode) if mode!='floor' else self.over(i,BONE,x,y)
    def skulls(self,x,y):self.add(0x1AD9,BONE,x,y)
    def leviathan(self,x,y):
        self.add(0x1AEE,BONE,x,y);self.add(0x3DBC,BONE,x+2,y);self.add(0x3DBD,BONE,x+3,y+1);self.add(0x3DBE,BONE,x+5,y);self.add(0x3DC0,BONE,x+4,y+1);self.add(0x3DC4,BONE,x+1,y+1)
    # --- walls ---
    def painting(self,x,y,west=False,i=None):
        (self.west if west else self.north)(i if i else (0x3321 if west else 0x2A5D),SHELF,x,y)
    def banner(self,x,y,west=False,i=None):(self.west if west else self.north)(i if i else (0x1586 if west else 0x1587),STONE,x,y)
    def curtain(self,x,y,west=False,tall=True):(self.west if west else self.north)((0x12DB if west else 0x160E) if tall else 0x980,ITEM,x,y)
    def tapestry(self,x,y,west=False,i=None):(self.west if west else self.north)(i if i else (0xEAA if west else 0xEAB),SHELF,x,y)
    def window(self,x,y,west=False):(self.west if west else self.north)(0x155 if west else 0x156,TEAL,x,y)
    def trophy(self,x,y,west=False,i=None):(self.west if west else self.north)(i if i else (0x337D if west else 0x337F),0,x,y,-118)
    def net(self,x,y,west=False):(self.west if west else self.north)(0x3D8E if west else 0x3D8F,0,x,y)
    def wallmap(self,x,y,west=False):(self.west if west else self.north)(0x2D73 if west else 0x2D74,SHELF,x,y,-116)
    def shield(self,x,y,west=False,i=None):(self.west if west else self.north)(i if i else 0x1570,ARMOR,x,y,-116)
    def weapons(self,x,y,west=False):(self.west if west else self.north)(0x155E if west else 0x1564,ARMOR,x,y)
    def wheel(self,x,y,west=False):(self.west if west else self.north)(0x46FC if west else 0x46FD,0,x,y,-116)
    def chains(self,x,y,west=False):(self.west if west else self.north)(0x1A08,BONE,x,y,-115)

def pool_footprint(x,y,large=False):return {(x+dx,y+dy) for dx,dy,_ in (POOL_LARGE if large else POOL_SMALL)}

def author_rooms():
    rooms=[]
    def room(d,n):
        r=Room(d,n);rooms.append(r);return r

    # ---------------------------------------------------------------- NORTH
    r=room('north','tidal_archive')            # 4320-4350 x 2520-2534, north wall
    for x in (4321,4322,4324,4325):r.shelf(x,2520,vines=x in (4322,4325))
    for x in (4337,4338,4340,4341):r.bookcase(x,2520,vines=x in (4337,4341))
    r.bookcase(4343,2520,dusty=True);r.shelf(4344,2520,broken=True)
    r.painting(4330,2520);r.wallmap(4332,2520);r.banner(4334,2520);r.painting(4346,2520,i=0x3E11);r.banner(4348,2520)
    r.lamp(4327,2520);r.lamp(4336,2520,sconce=True);r.lamp(4345,2520);r.lamp(4323,2520,sconce=True)
    r.add(0x89B,WOOD,4329,2521);r.add(0x65D4,ITEM,4335,2521)
    r.ornate(4323,2525,6,4);r.table(4324,2526,4,'marble',contents=((0x3B59,0),(0xA27,1,SETTING),(0x14EB,2),(0x1810,3)))
    for k in (0,2):r.chair(4324+k,2525,'s',hue=OLIVE);r.chair(4325+k,2527,'n',hue=OLIVE)
    r.mosaic(4333,2525,6,4,STONE);r.table(4334,2526,4,'marble',contents=((0xFF1,0),(0x1E21,1),(0x3031,2),(0x14EC,3)))
    r.marble_bench(4334,2525);r.marble_bench(4336,2525);r.marble_bench(4335,2527);r.marble_bench(4337,2527)
    r.desk(4343,2526);r.candelabra(4341,2528);r.candelabra(4330,2528,True)
    r.writing_table(4328,2531,((0x2051,),(0x3B5A,)));r.chair(4328,2532,'n',hue=OLIVE);r.skeleton(4327,2532)
    r.over(0xC16,ITEM,4326,2532);r.over(0xE35,ITEM,4329,2533);r.over(0x1E20,ITEM,4330,2532)
    r.add(0x1047,ITEM,4340,2531);r.crate(4320,2521,0x4F87,0x3B59);r.crate(4320,2523,0x4F8A,0x1E21);r.chest(4321,2533,0x4FF5,0)
    r.add(0x9AB,ARMOR,4320,2532);r.urn(4322,2533,0x1AFE);r.urn(4336,2532,0x1AFC)
    r.mushrooms(4345,2531);r.moss(4342,2532);r.bones(4347,2530,0xECE);r.kelp(4349,2522,False);r.crab(4346,2533,0x44D1);r.over(0x79A7,0,4348,2531)
    r.statue(4349,2521,0x4FBF);r.statue(4349,2527,0x42BC);r.coral(4350,2532,0x6596);r.glow(4343,2533)
    r.sinkhole(4333,2532,0x30EA);r.cracks(4338,2530)

    r=room('north','city_watch_armory')        # 4320-4343 x 2552-2574
    for i,x in [(0x1564,4321),(0x1565,4322),(0xA9D,4324),(0x3C00,4325),(0x3CF0,4326),(0x1568,4336),(0x1569,4337)]:r.north(i,ARMOR,x,2552)
    r.north(0x151A,ARMOR,4327,2552);r.north(0x151B,ARMOR,4335,2552);r.north(0x3D87,ARMOR,4339,2552);r.north(0x156C,ARMOR,4341,2552)
    r.shield(4342,2552,i=0x1577);r.lamp(4340,2552);r.lamp(4323,2552,sconce=True)
    for i,y in [(0x155E,2555),(0x5010,2557),(0x506A,2558),(0x155F,2570),(0x1376,2572)]:r.west(i,ARMOR,4320,y)
    r.lamp(4320,2553,True);r.lamp(4320,2573,True,True);r.shield(4320,2569,True,0x1579)
    r.ornate(4329,2553,3,8,STONE)
    for i,x,y in [(0x1071,4326,2557),(0x1070,4332,2557),(0x3DA9,4338,2554),(0x1377,4337,2562),(0x64F0,4322,2565),(0xF38,4338,2564)]:r.add(i,ARMOR,x,y)
    r.add(0xFB1,ARMOR,4321,2566);r.add(0xE7B,ITEM,4323,2566);r.over(0x1BE5,ARMOR,4322,2567);r.over(0x1BF0,ARMOR,4324,2567)
    r.table(4324,2564,3,'log',contents=((0x1412,0,VERDIGRIS),(0xFB4,1,ARMOR),(0x1B76,2,VERDIGRIS)))
    r.brazier(4327,2559);r.brazier(4335,2559);r.add(0x1ED6,0,4341,2556);r.over(0xF3F,0,4340,2558);r.over(0xF3E,0,4341,2559)
    r.crates(4340,2572,3);r.add(0x4BD0,ITEM,4342,2570);r.barrel(4338,2573);r.barrel(4339,2573,0x1097);r.add(0x1AD7,ITEM,4337,2573)
    r.bench(4324,2570);r.bench(4331,2570);r.desk(4333,2570,scholar=False)
    r.skeleton(4325,2573,0x123F);r.over(0x1B76,VERDIGRIS,4326,2573);r.over(0x1415,VERDIGRIS,4324,2574)
    r.growth(4336,2566);r.moss(4333,2568);r.shells(4334,2568);r.coral(4342,2569,0x6596);r.crab(4341,2568,0x531F);r.ruin(4321,2560)
    r.rubble(4342,2560);r.statue(4341,2553,0x4FA7);r.add(0xC2E,ITEM,4340,2567);r.sinkhole(4330,2566,0x30EE)

    r=room('north','navigators_chartroom')     # 4368-4390 x 2544-2550
    r.bookcase(4369,2544,dusty=True);r.wallmap(4371,2544);r.wallmap(4375,2544);r.north(0x3BB6,SHELF,4379,2544,-116);r.painting(4386,2544,i=0x331E);r.bookcase(4389,2544,dusty=True)
    r.lamp(4373,2544);r.lamp(4383,2544,sconce=True);r.lantern(4377,2544);r.banner(4381,2544)
    r.writing_table(4371,2546,((0x14EB,),(0xFC0,)));r.chair(4371,2547,'n','wood');r.over(0x14EE,ITEM,4370,2547)
    r.table(4374,2546,3,'wood',contents=((0x14EC,0),(0x14ED,1),(0x3031,2)));r.chair(4375,2547,'n','wood')
    r.small_table(4381,2546,contents=((0x1057,),));r.over(0x14F5,ITEM,4382,2546);r.add(0x1047,ITEM,4383,2546)
    r.add(0x1459,ITEM,4386,2546);r.mosaic(4384,2545,5,3,STONE);r.chair(4385,2547,'n',hue=OLIVE)
    r.table(4369,2549,3,'marble',contents=((0x14F3,0),(0x14F1,1),(0x1810,2)));r.crate(4373,2549,0x4F8B,0xE35);r.barrel(4374,2549,0xE77)
    r.ornate(4383,2548,6,2);r.marble_bench(4384,2549);r.marble_bench(4386,2549);r.urn(4389,2549,0x1AFD);r.add(0x1EB5,ITEM,4389,2545)
    r.kelp(4368,2550);r.moss(4369,2550,((0,0,0x546),(1,0,0x548)));r.crab(4388,2550);r.fishes(4390,2548);r.over(0x1B45,0,4390,2550)
    r.statue(4390,2545,0x4578);r.over(0xE34,ITEM,4372,2548);r.cracks(4378,2548)

    r=room('north','officers_refectory')       # 4400-4423 x 2536-2550, bay 4408-4423 x 2552-2557
    r.curtain(4404,2536);r.window(4406,2536);r.banner(4408,2536);r.trophy(4410,2536,i=0x52C5);r.curtain(4412,2536);r.banner(4416,2536);r.window(4418,2536);r.curtain(4420,2536)
    r.lamp(4402,2536,sconce=True);r.lamp(4414,2536);r.lamp(4422,2536,sconce=True);r.shield(4409,2536,i=0x1571);r.shield(4415,2536,i=0x1577)
    r.west(0x3321,SHELF,4400,2541);r.lamp(4400,2545,True);r.west(0x3E7,ITEM,4400,2548);r.curtain(4400,2538,True)
    r.ornate(4404,2540,10,5)
    r.table(4405,2542,7,'log',contents=((0x9D7,0,SETTING),(0x9C9,1),(0x9BF,2,SETTING),(0xA27,3,SETTING),(0x9B7,4),(0x9BF,5,SETTING),(0x9D7,6,SETTING)))
    for k in range(7):
        if k%2==0:r.wood_bench(4405+k,2541);r.wood_bench(4405+k,2543,True)
    r.table(4401,2544,4,'marble',west=True,contents=((0x2D4F,0),(0x9A7,1),(0x24D7,2),(0xE48,3)))
    r.add(0x1AD6,ITEM,4402,2548);r.add(0x1AD7,ITEM,4403,2548);r.barrel(4402,2549,0x154D);r.add(0x974,ARMOR,4401,2549)
    r.over(0x9E3,ITEM,4402,2547);r.over(0x9F3,ITEM,4403,2547);r.brazier(4401,2537);r.brazier(4422,2537)
    r.statue(4421,2540,0x4578);r.statue(4421,2544,0x457B);r.rubble(4420,2547);r.add(0x11C9,PLANT,4422,2550);r.moss(4418,2549);r.shells(4420,2549);r.sinkhole(4422,2548,0x30F2)
    r.table(4414,2547,3,'wood',contents=((0x9AE,0),(0xFA7,1),(0x999,2)));r.chair(4415,2546,'s','wood');r.chair(4415,2548,'n','wood');r.chair(4413,2547,'e','wood')
    r.add(0xC11,WOOD,4418,2549);r.candelabra(4409,2549);r.candelabra(4419,2545,True)
    # southern bay: off-duty quarters
    r.add(0x11F3,WOOD,4421,2553);r.add(0x11F2,WOOD,4421,2554);r.add(0x11FD,BONE,4414,2556);r.add(0x11FE,BONE,4415,2556)
    r.small_table(4417,2553,contents=((0xE12,),(0x3031,)));r.chair(4416,2553,'e','wood');r.chair(4418,2553,'w','wood')
    r.crate(4409,2556,0x4F8F);r.add(0xA22,0,4410,2556);r.add(0x10EE,ITEM,4412,2557);r.over(0x13A4,ITEM,4420,2556);r.skeleton(4412,2554,0x123F);r.over(0xF02,ITEM,4413,2554)
    r.mushrooms(4409,2553);r.bones(4410,2554,0xECC);r.moss(4419,2557);r.kelp(4422,2557)

    r=room('north','watch_barracks')           # 4352-4374 x 2552-2574; west openings 2560-2566
    for k in range(5):
        r.add(0x11FD,BONE,4372,2553+k*3);r.add(0x11FE,BONE,4373,2553+k*3);r.add(0xE7D,SHELF,4371,2553+k*3)
    r.surf(0x1236,BONE,4372,2556,-125);r.surf(0x123F,BONE,4373,2565,-125);r.over(0x13A5,ITEM,4372,2559)
    r.add(0x11F3,WOOD,4353,2570);r.add(0x11F2,WOOD,4353,2571);r.add(0x11F3,WOOD,4356,2570);r.add(0x11F2,WOOD,4356,2571)
    r.north(0xC39,ITEM,4354,2552);r.weapons(4356,2552);r.north(0x1566,ARMOR,4357,2552);r.shield(4359,2552);r.banner(4370,2552);r.north(0xC3A,ITEM,4374,2552)
    r.lamp(4353,2552);r.lamp(4372,2552,sconce=True);r.lamp(4367,2552);r.window(4368,2552);r.lantern(4358,2552)
    r.table(4358,2556,5,'wood',contents=((0x998,0),(0xFA7,1),(0x9BF,2,SETTING),(0x0F02,3),(0x9AE,4)))
    r.wood_bench(4358,2555);r.wood_bench(4360,2555);r.wood_bench(4362,2555);r.wood_bench(4359,2557,True);r.wood_bench(4361,2557,True)
    r.add(0x3DA9,ARMOR,4366,2556);r.add(0x1071,ARMOR,4366,2559);r.over(0xF3E,0,4367,2560)
    r.chest(4353,2553);r.chest(4354,2553,0xE43);r.crate(4355,2554,0x4F90,0x1412);r.barrel(4353,2555,0x1097)
    r.mosaic(4356,2561,8,5,STONE,True);r.brazier(4358,2560,True);r.brazier(4363,2560,True)
    r.add(0x1070,ARMOR,4360,2563);r.stone_bench(4364,2564);r.stone_bench(4364,2566,True)
    r.moss(4353,2557);r.mushrooms(4354,2559);r.moss(4359,2566,hue=RUG);r.kelp(4370,2569,False);r.crab(4369,2572,0x65C0);r.over(0x1A93,0,4355,2558)
    r.skeleton(4361,2572,0x123F);r.over(0xF45,VERDIGRIS,4362,2572);r.add(0xC10,WOOD,4368,2573);r.candelabra(4366,2570)
    r.barrel(4358,2573,0x1DB4);r.barrel(4359,2573,0x1DB5);r.over(0x1037,1881,4360,2574);r.sinkhole(4366,2567,0x30F2);r.cracks(4362,2568)

    r=room('north','bath_house')               # 4384-4406 x 2552-2574
    r.paving_ring(4386,2554,19,19)
    r.pool(4390,2558,True,jelly=3);r.pool(4391,2565,True,life=None);r.pool(4396,2561,True,'fish',jelly=1)
    r.over(0x5D1F,SEA,4397,2565);r.over(0x5D29,SEA,4398,2566);r.over(0x5D1F,SEA,4398,2567)
    r.fountain(4394,2564);r.over(0xD0B,0,4393,2569);r.over(0xD09,0,4399,2563);r.fishes(4392,2562);r.over(0x540D,2966,4398,2569)
    r.add(0x6215,0,4392,2569);r.over(0x79A7,0,4399,2560)
    for x,y in ((4385,2553),(4405,2553),(4385,2573),(4405,2573)):r.brazier(x,y)
    for x in (4388,4402):r.statue(x,2553,0x457A if x<4400 else 0x457B)
    r.statue(4386,2563,0x4578);r.statue(4404,2563,0x4579);r.statue(4395,2573,0x4FBF)
    for x in (4390,4400):r.marble_bench(x,2555);r.marble_bench(x,2571)
    for y in (2558,2568):r.marble_bench(4386,y,True);r.marble_bench(4404,y,True)
    r.add(0x2D0B,MARBLE,4387,2556);r.add(0x30DF,MARBLE,4403,2556);r.add(0xB41,ITEM,4387,2570);r.add(0xB42,ITEM,4403,2570)
    r.urn(4389,2554,0x1AFE);r.urn(4401,2554,0x1AFD);r.urn(4388,2572);r.urn(4402,2572,0x1AFE);r.vase(4395,2554,0x42B2);r.vase(4395,2555,0xB47)
    r.over(0x13A4,ITEM,4391,2556);r.over(0x13A5,ITEM,4400,2570);r.over(0x1008,ITEM,4388,2556);r.over(0x9A7,ITEM,4403,2557)
    r.kelp(4385,2560);r.kelp(4405,2560);r.kelp(4385,2566);r.kelp(4405,2566);r.coral(4387,2573,0x6595);r.coral(4403,2573,0x6597);r.coral(4405,2569,0x6596)
    r.crab(4388,2565,0x44D1);r.crab(4402,2559);r.over(0x25F5,0,4392,2571);r.shells(4396,2554);r.crystal(4388,2559,0x61DA);r.crystal(4402,2569,0x61E1)
    r.lamp(4387,2552);r.lamp(4391,2552,sconce=True);r.lamp(4399,2552,sconce=True);r.lamp(4405,2552);r.curtain(4389,2552);r.curtain(4401,2552)
    r.column(4389,2557,True);r.rubble(4404,2557);r.over(0x1B45,0,4390,2572);r.cracks(4386,2568);r.sinkhole(4404,2572,0x30F2)

    for half,x0 in (('west',4424),('east',4448)):
        r=room('north','hall_of_the_leviathan_'+half)      # 4424-4470 x 2536-2542
        for k in range(6):
            x=x0+k*4
            if x>x0+20:break
            r.column(x,2538,broken=k%3==2);r.column(x,2541,broken=k%3==1)
            if k%2==0:r.banner(x+1,2536,i=(0x1588,0x1589,0x158A)[k%3]);r.lamp(x+3,2536,sconce=k%4==0)
            else:
                r.tapestry(x+1,2536,i=0xFD7)
                if k<5:r.lamppost(x+2,2542,True)
        r.ornate(x0,2539,21,2,SEA)
        if half=='west':
            r.leviathan(x0+7,2539);r.statue(x0+2,2540,0x31C8);r.statue(x0+18,2537,0x31CB);r.skulls(x0+9,2541);r.bones(x0+13,2540,0xECF)
            r.crystal(x0+15,2537,0x61DA);r.shells(x0+9,2542);r.moss(x0+4,2541);r.crab(x0+11,2537,0x531F);r.sinkhole(x0+6,2537,0x30EA);r.bones(x0+16,2541,0xED0)
        else:
            r.statue(x0+1,2537,0x31CD);r.statue(x0+19,2540,0x31D0);r.statue(x0+10,2537,0x4D0A,VERDIGRIS)
            r.rubble(x0+5,2540);r.skulls(x0+12,2537);r.mushrooms(x0+13,2541);r.growth(x0+3,2541);r.over(0x25F5,0,x0+8,2542);r.coral(x0+16,2542,0x6597);r.crystal(x0+20,2542)
            r.add(0x1AEF,BONE,x0+7,2541);r.add(0x3DC8,BONE,x0+9,2541);r.cracks(x0+14,2541)

    r=room('north','harbour_storehouse')       # 4408-4430 x 2568-2582; west wall 4407, north wall 2567
    r.deck(4409,2569,9,5)
    for k,(dx,dy) in enumerate(((0,0),(1,0),(2,0),(0,1),(2,1),(1,2))):r.crate(4410+dx,2570+dy,CRATES[k])
    r.add(0x4BD0,ITEM,4414,2570);r.add(0x96D,ITEM,4416,2569);r.add(0xAA6,ITEM,4416,2572);r.add(0x5082,ITEM,4415,2573)
    r.barrel(4409,2574);r.barrel(4410,2574,0x1097);r.barrel(4411,2574,0xE77);r.barrel(4413,2574,0x1EB5);r.add(0x1039,ITEM,4412,2573);r.add(0x1046,ITEM,4409,2573)
    r.add(0x1248,ITEM,4417,2574);r.add(0x1C10,ITEM,4418,2573);r.add(0x24D9,ITEM,4409,2570);r.add(0x65BB,ITEM,4409,2571)
    r.net(4408,2569,True);r.net(4425,2568);r.wheel(4420,2568);r.north(0x3D8E,0,4413,2568);r.lamp(4411,2568);r.lamp(4426,2568,sconce=True);r.lantern(4417,2568);r.banner(4429,2568,i=0x427)
    r.add(0x14F7,0,4426,2569);r.over(0x14F8,0,4427,2570);r.over(0x14FA,0,4425,2571);r.add(0x46B6,0,4428,2570);r.over(0x44CB,0,4429,2571)
    r.add(0x3EDD,0,4422,2573);r.add(0x7B9C,0,4423,2575);r.add(0x7B9E,0,4425,2574);r.over(0xDCA,0,4420,2571);r.over(0xDC9,0,4421,2572);r.over(0xDD2,0,4424,2576)
    r.table(4419,2577,3,'log',contents=((0x14F3,0),(0x1851,1),(0x14F1,2)));r.chair(4420,2578,'n','wood');r.add(0x1EB5,ITEM,4423,2578)
    r.add(0xA1F,ITEM,4408,2576);r.add(0xA1F,ITEM,4430,2576);r.add(0x880,PED,4413,2575);r.add(0x880,PED,4414,2575);r.add(0x93,PED,4412,2575)
    r.skeleton(4415,2577,0x123F);r.over(0xF7C,ITEM,4416,2577);r.crates(4427,2579,7);r.add(0x1DB4,ITEM,4426,2581);r.over(0x1038,1881,4417,2580)
    r.over(0x52E1,0,4410,2578);r.kelp(4430,2582);r.shells(4409,2581);r.moss(4421,2581);r.crab(4424,2581,0x65C0);r.crab(4411,2579,0x44D1);r.over(0x52C9,0,4419,2582);r.sinkhole(4413,2580,0x30F2)
    r.rubble(4428,2573);r.coral(4408,2580,0x6596);r.sinkhole(4425,2580,0x30EE);r.add(0x229A,0,4430,2579)

    r=room('north','refectory_kitchen')        # 4375-4390 x 2529-2542 (wall y=2527 west of 4374; keep margin)
    r.add(0x975,ARMOR,4376,2529);r.add(0x974,ARMOR,4389,2529);r.north(0xC39,ITEM,4380,2528);r.north(0xC3A,ITEM,4384,2528);r.lamp(4378,2528);r.lamp(4387,2528,sconce=True);r.curtain(4382,2528,tall=False)
    r.table(4377,2532,5,'log',contents=((0x9E0,0),(0x1E94,1),(0x97B,2),(0x9C9,3),(0x103B,4)))
    r.table(4384,2532,4,'wood',contents=((0x9F3,0),(0x9EC,1,PED),(0x1E1D,2),(0x9CC,3,SHELF)))
    r.add(0xE7B,ITEM,4376,2534);r.barrel(4389,2534,0x154D);r.add(0x1039,ITEM,4388,2534);r.add(0x1046,ITEM,4387,2534)
    r.add(0x24D7,ITEM,4377,2536);r.add(0x65BB,ITEM,4378,2536);r.add(0xE44,ITEM,4379,2536);r.add(0xE4C,ITEM,4380,2536);r.add(0x1AD6,ITEM,4389,2537);r.add(0x1AD7,ITEM,4388,2537)
    r.table(4381,2538,3,'stone',contents=((0x9CE,0,SHELF),(0x1E17,1,0),(0x1E1A,2,0)));r.add(0xA2A,WOOD,4382,2539);r.add(0x10F2,ITEM,4376,2540)
    r.chest(4389,2540,0xE7E,SHELF);r.crate(4389,2541,0x4F94);r.over(0x9B9,ITEM,4383,2540);r.over(0x1E1B,0,4384,2540)
    r.skeleton(4378,2540,0x123F);r.over(0x1443,ITEM,4379,2541);r.crab(4386,2536);r.crab(4381,2541,0x44D1);r.fishes(4386,2541)
    r.mushrooms(4385,2538);r.over(0x1E1B,0,4386,2539);r.moss(4376,2541,hue=RUG);r.growth(4389,2541);r.candelabra(4375,2537,True);r.sinkhole(4381,2535,0x30EA)

    # -------------------------------------------------------------- CENTRAL
    r=room('central','scriptorium')            # 4336-4350 x 2600-2606
    r.bookcase(4338,2600,dusty=True);r.bookcase(4339,2600,dusty=True);r.painting(4342,2600);r.bookcase(4345,2600);r.bookcase(4346,2600,dusty=True);r.lamp(4344,2600);r.lamp(4348,2600,sconce=True);r.lantern(4341,2600)
    r.west(0x3321,SHELF,4336,2603);r.lamp(4336,2605,True);r.curtain(4336,2601,True)
    r.rug(4338,2602,6,4,((0,3),(5,0)))
    r.writing_table(4339,2603,((0x2051,),(0xE34,)));r.add(0xC17,MARBLE,4339,2604)
    r.writing_table(4342,2603,((0xFC0,),(0x3B54,)));r.chair(4342,2604,'n','wood');r.over(0xE36,ITEM,4343,2604)
    r.add(0xF66,WOOD,4349,2601);r.add(0xA2A,WOOD,4348,2602);r.add(0x1047,ITEM,4349,2604);r.crate(4337,2600,0x4F8B,0xEF4)
    r.table(4345,2605,3,'marble',contents=((0x65D5,0),(0x1430,1),(0x1E21,2)))
    r.over(0xC16,ITEM,4341,2606);r.over(0xE37,ITEM,4346,2602);r.over(0x1E20,ITEM,4344,2602);r.skeleton(4340,2605);r.over(0x3B59,ITEM,4341,2605)
    r.sinkhole(4349,2606,0x30EE);r.moss(4348,2605,((0,0,0x546),(1,0,0x548)));r.kelp(4350,2606,False);r.crab(4347,2606);r.urn(4350,2600,0x1AFE);r.candelabra(4337,2606)

    r=room('central','council_of_tides')       # 4360-4379 x 2608-2621; west wall x=4359
    r.mosaic(4362,2609,15,12,STONE,holes={(7,5),(7,6),(6,5),(6,6),(8,5),(8,6),(6,7),(7,7),(8,7)})
    r.altar_disc(4368,2614)
    r.golden_throne(4369,2610);r.table(4365,2610,3,'marble',contents=((0x14EC,0),(0x3031,1),(0x14EB,2)));r.table(4371,2610,3,'marble',contents=((0x1E21,0),(0x3031,1),(0x65D8,2)))
    for x in (4364,4366,4372,4374):r.chair(x,2612,'s',hue=OLIVE)
    for x,y in ((4363,2615),(4363,2617),(4375,2615),(4375,2617)):r.marble_bench(x,y,True)
    for x in (4365,4367,4371,4373):r.chair(x,2619,'n',hue=OLIVE)
    r.add(0x65D4,ITEM,4369,2618)
    for x,y,i in ((4360,2608,0x31D1),(4377,2608,0x31D4),(4360,2621,0x3182),(4377,2621,0x40BC)):r.statue(x,y,i)
    r.brazier(4362,2608);r.brazier(4375,2608);r.brazier(4362,2621);r.brazier(4375,2621)
    r.banner(4360,2610,True,0x1586);r.banner(4360,2613,True,0x158F);r.lamp(4360,2611,True);r.tapestry(4360,2609,True)
    r.pool(4371,2616,True,jelly=2);r.crystal(4377,2616,0x61DD);r.kelp(4377,2619);r.moss(4361,2618);r.growth(4361,2615);r.crab(4377,2613,0x44D1);r.over(0x25F5,0,4373,2621)
    r.rubble(4361,2612);r.over(0x1B4A,0,4364,2614);r.urn(4377,2610,0x1AFE);r.vase(4361,2620,0x42B2);r.cracks(4370,2620);r.sinkhole(4366,2621,0x30EA)

    r=room('central','shrine_of_the_deep')     # 4352-4374 x 2624-2630; west wall x=4351
    r.dais(4361,2625,3,2);r.on_dais(0x4FC2,0,4362,2625);r.add(0x6395,MARBLE,4360,2626);r.statue(4362,2624,0x4FB1)
    for k,i in enumerate((0x4881,0x484A,0x4883,0x4881)):r.over(i,CRYS,(4358,4366,4358,4366)[k],(2625,2625,2629,2629)[k])
    r.brazier(4359,2624);r.brazier(4365,2624);r.candelabra(4359,2628);r.candelabra(4365,2628,True)
    r.statue(4356,2624,0x4578);r.statue(4368,2624,0x4579);r.statue(4354,2628,0x457A);r.statue(4370,2628,0x457B)
    r.mosaic(4357,2624,11,6,DARK,holes={(x,y) for x in range(4,7) for y in range(1,3)})
    r.over(0x3196,0,4361,2628);r.over(0x1B45,0,4362,2628);r.over(0xEF1,0,4363,2628);r.over(0xEEE,0,4360,2629);r.over(0xE98,0,4364,2629);r.over(0x1B4F,0,4362,2629)
    r.add(0x2FEF,SHELF,4358,2627);r.add(0x2FF0,SHELF,4366,2627);r.add(0x398B,0,4357,2630);r.add(0x39A2,0,4367,2630)
    r.curtain(4352,2625,True,False);r.curtain(4352,2629,True,False);r.lamp(4352,2627,True);r.painting(4352,2626,True,0x3319)
    r.pool(4353,2624,life='lily');r.growth(4369,2625);r.shells(4370,2627);r.kelp(4352,2630);r.growth(4356,2630);r.crab(4373,2627,0x65C0);r.fishes(4371,2630)
    r.crystal(4353,2630,0x61DA);r.glow(4368,2630);r.glow(4356,2629);r.coral(4374,2624,0x6595);r.column(4355,2626,True);r.column(4369,2629,True)
    r.bones(4372,2629,0xECF);r.over(0x1B4A,0,4362,2630)

    r=room('central','gatehouse_watch')        # 4343-4358 x 2616-2622; north wall y=2615 (gap 4351)
    r.weapons(4344,2616);r.north(0x1565,ARMOR,4345,2616);r.shield(4347,2616,i=0x1571);r.chains(4353,2616);r.chains(4355,2616);r.lamp(4349,2616);r.lamp(4357,2616,sconce=True)
    r.table(4344,2618,3,'wood',contents=((0xFA7,0),(0x998,1),(0xE12,2)));r.chair(4345,2617,'s','wood');r.chair(4345,2619,'n','wood');r.chair(4347,2618,'w','wood')
    r.add(0x2019,CRATE,4353,2618);r.add(0x2019,CRATE,4356,2618);r.skeleton(4354,2619,0x123F);r.skeleton(4357,2619);r.skulls(4355,2619);r.over(0x3A4,CRATE,4353,2620);r.over(0x3A4,CRATE,4356,2620)
    r.stone_bench(4349,2620);r.brazier(4351,2621,True);r.barrel(4343,2621,0x1097);r.over(0x1037,1881,4344,2621);r.add(0x1AD7,ITEM,4343,2620)
    r.moss(4350,2617,hue=RUG);r.sinkhole(4357,2621,0x30EA);r.kelp(4358,2622,False);r.crab(4348,2622,0x531F);r.bones(4352,2622,0xECB)

    r=room('central','fishmongers_arcade')     # 4322-4350 x 2584-2590; north wall y=2583
    for k,x in enumerate((4323,4331,4339,4347)):
        r.table(x,2586,3,'log',contents=(((0x9CC,0,SHELF),(0x9CD,1,SHELF),(0x1851,2)),((0x1E15,0,0),(0x1E16,1,0),(0x9CE,2,SHELF)),((0x22AD,0,0),(0x9CF,1,SHELF),(0x1E19,2,0)),((0xDD6,0,0),(0x24D7,1),(0x9CD,2,SHELF)))[k])
        r.add(0x1297 if k%2 else 0x1298,SHELF,x+1,2585);r.lamppost(x-1,2587)
        r.barrel(x+3,2586,(0xFAE,0x154D,0x1097,0xE77)[k]);r.over((0xDC9,0xDCA,0xDD0,0xDD3)[k],0,x+1,2588)
    r.north(0x426,SHELF,4326,2584);r.north(0x42A,SHELF,4336,2584);r.lamp(4330,2584);r.lamp(4342,2584,sconce=True);r.trophy(4333,2584,i=0x3381);r.trophy(4345,2584,i=0x52C7);r.net(4340,2584);r.lantern(4348,2584)
    r.add(0x24D9,ITEM,4325,2588);r.add(0x65BB,ITEM,4333,2588);r.add(0x1039,ITEM,4341,2588);r.crate(4349,2588,0x4F92,0x9CC);r.add(0x96D,ITEM,4322,2585)
    r.over(0xDBF,0,4328,2589);r.over(0xDC0,0,4336,2589);r.over(0xDCC,0,4344,2589);r.over(0x52E1,0,4331,2589);r.over(0x52C9,0,4347,2589);r.over(0x25F5,0,4338,2585)
    r.skeleton(4335,2589,0x123F);r.over(0xEEB,0,4336,2590);r.crab(4324,2590,0x44D1);r.crab(4343,2590);r.fishes(4329,2585);r.fishes(4346,2590)
    r.pool(4342,2584,life='fish',jelly=3);r.moss(4339,2589,hue=RUG);r.kelp(4350,2590);r.growth(4349,2585);r.sinkhole(4333,2590,0x30EE);r.over(0x1B4A,0,4341,2590)

    # ----------------------------------------------------------------- EAST
    r=room('east','keepers_library')           # 4400-4422 x 2592-2598; north wall from 4407
    for x in range(4408,4422):
        if x in (4413,4418):r.lamp(x,2592,sconce=x==4418)
        elif x==4416:r.add(0xC13,SHELF,x,2592)
        else:r.bookcase(x,2592,vines=x%3==0)
    r.add(0x89B,WOOD,4410,2593);r.over(0xC16,ITEM,4417,2593);r.over(0x1E21,ITEM,4409,2594)
    r.mosaic(4401,2594,6,4,STONE);r.table(4402,2595,4,'marble',contents=((0xA27,0,SETTING),(0x3B59,1),(0x1E20,2),(0x14ED,3)))
    r.chair(4402,2594,'s',hue=OLIVE);r.chair(4404,2594,'s',hue=OLIVE);r.chair(4403,2596,'n',hue=OLIVE);r.chair(4405,2596,'n',hue=OLIVE);r.skeleton(4403,2597)
    r.rug(4411,2594,6,4,((5,3),));r.table(4412,2595,4,'marble',contents=((0x3B5A,0),(0x1430,1),(0x65D5,2),(0xFF0,3)))
    r.chair(4412,2594,'s');r.chair(4414,2594,'s');r.chair(4413,2596,'n');r.chair(4415,2596,'n')
    r.statue(4421,2597,0x42BC);r.statue(4420,2593,0x12CB);r.add(0x1047,ITEM,4418,2597);r.candelabra(4400,2593);r.urn(4422,2594,0x1AFE)
    r.crate(4400,2592,0x4F88,0x3B59);r.crate(4400,2597,0x4F8D,0x1E21);r.moss(4407,2597,hue=RUG);r.kelp(4400,2598,False);r.crab(4409,2598);r.sinkhole(4419,2598,0x30EA);r.glow(4408,2597)

    r=room('east','curators_treasury')         # 4447-4463 x 2592-2598
    r.painting(4449,2592,i=0x3DF4);r.tapestry(4451,2592,i=0x234F);r.wallmap(4453,2592);r.painting(4456,2592,i=0x3DF7);r.tapestry(4459,2592,i=0x234E);r.painting(4462,2592,i=0x2A61)
    r.lamp(4448,2592);r.lamp(4455,2592,sconce=True);r.lamp(4461,2592)
    r.mosaic(4451,2594,6,4,SEA,True)
    r.add(0x879,0,4449,2597);r.add(0x8AD,0,4450,2598);r.over(0xEEE,0,4451,2598);r.over(0xEEF,0,4448,2596);r.over(0x1B45,0,4452,2598);r.over(0xE98,0,4448,2598);r.over(0xEF1,0,4451,2597)
    r.chest(4462,2597,0x2821,MARBLE);r.chest(4461,2598,0x2823,MARBLE);r.chest(4461,2596,0x5080,MARBLE);r.add(0x5082,ITEM,4460,2596);r.chest(4448,2593,0x52E2,0);r.chest(4448,2594,0x4FF5,0)
    r.table(4452,2595,4,'marble',contents=((0x64D1,0,0),(0x573E,1,0),(0x3196,2,0),(0x4FD6,3)))
    for x,y,i in ((4449,2594,0x42B2),(4457,2594,0x4042),(4461,2594,0x39BD),(4453,2598,0x12CB),(4458,2598,0x4043)):r.statue(x,y,i)
    r.add(0x2FEF,SHELF,4455,2598);r.add(0x2FF0,SHELF,4456,2598);r.add(0x176C,0,4460,2594);r.add(0x178A,0,4462,2593)
    r.skeleton(4459,2597,0x123F);r.over(0x14FB,0,4460,2597);r.brazier(4450,2593,True);r.brazier(4460,2593,True)
    r.moss(4448,2596,hue=RUG);r.kelp(4462,2598,False);r.crab(4454,2593,0x531F);r.cracks(4457,2597)

    r=room('east','sculpture_gallery')         # 4400-4414 x 2608-2614 (+4408-4414 x 2616-2617)
    r.paving(4404,2609,7,5)
    for x,y,i,h in ((4401,2608,0x31CB,MARBLE),(4407,2608,0x4D0A,VERDIGRIS),(4413,2608,0x31D1,MARBLE),(4401,2614,0x63C2,MARBLE),(4413,2614,0x40BD,MARBLE),(4407,2611,0x42C5,MARBLE)):r.statue(x,y,i,h)
    r.statue(4404,2613,0x499);r.statue(4410,2613,0x12A2);r.add(0x750,PED,4404,2608);r.over(0x1225,MARBLE,4405,2609);r.over(0x1226,MARBLE,4403,2609);r.add(0x136D,ROCK,4405,2608)
    r.tapestry(4400,2610,True);r.tapestry(4400,2612,True,0xEAC);r.lamp(4400,2609,True,True);r.lamp(4400,2613,True);r.north(0x3145,TEAL,4402,2608);r.north(0x3146,TEAL,4406,2608);r.lamp(4404,2608,sconce=True)
    r.marble_bench(4402,2611);r.marble_bench(4412,2611);r.crystal(4401,2612,0x61DA);r.coral(4413,2612,0x6595);r.kelp(4401,2610,False);r.moss(4411,2613)
    r.growth(4408,2613);r.crystal(4409,2612,0x61E1);r.crab(4402,2610,0x65C0);r.urn(4413,2610,0x1AFC);r.vase(4402,2613,0xB47);r.glow(4414,2613)
    r.column(4409,2616,True);r.column(4413,2617);r.add(0x136B,ROCK,4410,2617);r.sinkhole(4411,2616,0x30F2);r.growth(4408,2617)

    r=room('east','residential_music_room')    # 4432-4454 x 2600-2606
    r.curtain(4434,2600);r.painting(4437,2600,i=0x3DEF);r.tapestry(4439,2600,i=0xEAD);r.window(4450,2600);r.painting(4448,2600,i=0x3DEC);r.curtain(4453,2600);r.lamp(4436,2600);r.lamp(4452,2600,sconce=True);r.lamp(4447,2600)
    r.west(0x3319,SHELF,4432,2603);r.lamp(4432,2605,True)
    r.ornate(4436,2602,9,4,OLIVE)
    r.add(0xEB1,ITEM,4437,2603);r.over(0xEB3,ITEM,4439,2604);r.over(0xE9A,ITEM,4441,2603);r.over(0x194B,ITEM,4438,2605)
    r.small_table(4442,2603,contents=((0xE1C,),));r.chair(4441,2603,'e','wood');r.chair(4443,2603,'w','wood');r.add(0xC17,MARBLE,4443,2605)
    r.wood_bench(4433,2602);r.wood_bench(4433,2604);r.add(0xA4D,SHELF,4451,2601);r.add(0xA30,SHELF,4453,2601);r.add(0x11C9,PLANT,4453,2603);r.add(0x11CA,PLANT,4433,2606)
    r.add(0xC1F,ITEM,4446,2601);r.vase(4448,2602,0xB48);r.candelabra(4450,2604,True);r.over(0x13A4,ITEM,4447,2605);r.over(0x13A5,ITEM,4449,2605);r.skeleton(4445,2605);r.over(0x9BF,SETTING,4446,2606)
    r.mushrooms(4449,2602);r.add(0xEBD,ITEM,4450,2603);r.moss(4441,2605);r.kelp(4454,2606);r.crab(4444,2606,0x44D1);r.sinkhole(4440,2606,0x30EA)

    r=room('east','atrium')                    # 4432-4454 x 2609-2613
    r.pool(4440,2608,True,jelly=1);r.column(4436,2609);r.column(4450,2609,True);r.column(4436,2613,True);r.column(4450,2613)
    r.lamppost(4434,2611,True);r.lamppost(4453,2611,True);r.marble_bench(4438,2611,True);r.marble_bench(4448,2611,True)
    r.urn(4437,2609,0x1AFE);r.urn(4449,2613,0x1AFC);r.add(0x11C9,PLANT,4433,2609);r.add(0x11C9,PLANT,4454,2613);r.growth(4432,2612);r.growth(4453,2609);r.crab(4447,2609);r.over(0x25F5,0,4438,2613);r.shells(4451,2612);r.cracks(4444,2613)

    r=room('east','noble_bedchamber')          # 4432-4454 x 2616-2622
    r.add(0x1899,BONE,4433,2616);r.add(0x1898,BONE,4434,2616);r.add(0x189B,BONE,4433,2617);r.add(0x189A,BONE,4434,2617);r.surf(0x1236,BONE,4434,2617,-122)
    r.over(0x13A5,ITEM,4435,2617);r.add(0xA3C,SHELF,4437,2616);r.add(0xA30,SHELF,4439,2616);r.add(0xC13,SHELF,4441,2616);r.add(0x2D0B,MARBLE,4443,2616)
    r.curtain(4432,2618,True,False);r.curtain(4432,2621,True,False);r.painting(4432,2619,True,0x3DF8);r.lamp(4432,2620,True)
    r.ornate(4436,2618,8,4,SEA);r.writing_table(4444,2619,((0x1430,),(0x3B54,)));r.add(0xC17,MARBLE,4444,2620);r.add(0xEB1,ITEM,4448,2617)
    r.add(0x2F2F,BONE,4451,2620);r.add(0x2F30,BONE,4452,2620);r.add(0xC22,SHELF,4453,2616);r.add(0xA35,SHELF,4454,2616);r.add(0xC1A,WOOD,4449,2621)
    r.add(0x11FD,BONE,4438,2621);r.add(0x11FE,BONE,4439,2621);r.over(0x13A4,ITEM,4436,2621);r.over(0xF02,ITEM,4441,2622);r.over(0x9A7,SHELF,4445,2621)
    r.add(0x11CA,PLANT,4447,2616);r.vase(4450,2616,0x4043);r.candelabra(4436,2617,True);r.chest(4453,2622,0x2821,MARBLE);r.chest(4435,2622,0xE43)
    r.moss(4449,2618);r.mushrooms(4450,2619);r.moss(4442,2622);r.kelp(4454,2622);r.crab(4446,2622,0x531F);r.sinkhole(4433,2622,0x30F2);r.cracks(4440,2620)

    for half,y0 in (('north',2593),('south',2612)):
        r=room('east','agora_'+half)           # 4380-4398 x 2593-2609 / 2612-2630
        if half=='north':
            r.paving(4384,y0+3,11,11);r.fountain(4389,y0+8);r.pool(4386,y0+4,True,'lily');r.pool(4389,y0+9,True,'fish',jelly=2);r.over(0xD0B,0,4388,y0+7);r.over(0xD09,0,4393,y0+12)
            r.over(0x540D,2817,4387,y0+7);r.fishes(4392,y0+6);r.over(0x540D,2929,4394,y0+11)
            for x,y in ((4383,y0+2),(4395,y0+2),(4383,y0+14),(4395,y0+14)):r.column(x,y,broken=(x+y)%3==0)
            for x,y in ((4381,y0+1),(4397,y0+1),(4381,y0+15),(4397,y0+15)):r.lamppost(x,y,True)
            r.statue(4380,y0+8,0x31CD);r.statue(4398,y0+8,0x31D0);r.statue(4389,y0+1,0x4D0B,VERDIGRIS);r.statue(4389,y0+16,0x4FBF)
            r.marble_bench(4386,y0+1);r.marble_bench(4392,y0+1);r.marble_bench(4386,y0+16);r.marble_bench(4392,y0+16)
            r.add(0x11C9,PLANT,4382,y0+5);r.add(0x11C9,PLANT,4396,y0+11);r.growth(4381,y0+11);r.growth(4396,y0+4);r.kelp(4380,y0+3);r.kelp(4398,y0+13);r.kelp(4398,y0+3,False)
            r.coral(4384,y0+12,0x6595);r.coral(4394,y0+3,0x6597);r.crab(4385,y0+9,0x65C0);r.over(0x25F5,0,4393,y0+5);r.shells(4396,y0+14);r.crystal(4382,y0+13,0x61DA);r.crystal(4396,y0+2,0x61E1)
            r.rubble(4383,y0+16);r.sinkhole(4394,y0+10,0x30EA);r.cracks(4386,y0+13);r.urn(4384,y0+1,0x1AFE);r.urn(4394,y0+16,0x1AFD);r.over(0x1B45,0,4390,y0+3)
        else:
            r.dais(4386,y0+1,7,3);r.on_dais(0x4023,STONE,4389,y0+2);r.on_dais(0x52D,ITEM,4386,y0+1);r.on_dais(0x52E,ITEM,4392,y0+1);r.on_dais(0x1AFE,ITEM,4386,y0+3);r.on_dais(0x1AFC,ITEM,4392,y0+3)
            r.brazier(4384,y0+2);r.brazier(4394,y0+2);r.statue(4384,y0,0x31C8);r.statue(4394,y0,0x31D4)
            r.mosaic(4385,y0+5,9,10,STONE,True);r.marble_bench(4386,y0+7,True);r.marble_bench(4392,y0+7,True);r.marble_bench(4386,y0+11,True);r.marble_bench(4392,y0+11,True)
            for x in (4383,4395):
                for y in (y0+6,y0+10,y0+14):r.column(x,y,broken=(x+y)%4==0)
            r.lamppost(4381,y0+8,True);r.lamppost(4397,y0+8,True);r.lamppost(4381,y0+16,True);r.lamppost(4397,y0+16,True)
            r.leviathan(4386,y0+16);r.pool(4380,y0+10,life='octopus');r.pool(4392,y0+12,life='bones')
            r.kelp(4380,y0+5);r.kelp(4398,y0+5);r.kelp(4398,y0+17);r.growth(4380,y0+17);r.coral(4398,y0+12,0x6596);r.over(0x25F5,0,4389,y0+9);r.crab(4396,y0+15,0x44D1);r.crab(4382,y0+13)
            r.crystal(4380,y0+14,0x61DD);r.glow(4389,y0+13);r.rubble(4396,y0+18);r.sinkhole(4384,y0+16,0x30F2);r.cracks(4394,y0+13);r.skulls(4391,y0+18);r.bones(4383,y0+18,0xECC)

    # ---------------------------------------------------------------- SOUTH
    r=room('south','mariners_mess')            # bay 4400-4406 x 2640-2646; hall 4408-4427 x 2640-2654
    r.net(4402,2640);r.trophy(4404,2640,i=0x655C);r.lamp(4406,2640);r.lantern(4400,2641,True);r.lamp(4400,2645,True);r.wheel(4400,2643,True)
    r.add(0x11F3,WOOD,4402,2643);r.add(0x11F2,WOOD,4402,2644);r.add(0x11F3,WOOD,4405,2643);r.add(0x11F2,WOOD,4405,2644);r.chest(4401,2646,0x4FF5,0);r.barrel(4406,2646,0x1097);r.add(0x14F7,0,4400,2646);r.over(0x14F8,0,4401,2641)
    r.rug(4410,2642,13,9,((0,0),(0,1),(12,8),(11,8)))
    # broken banquet table observed at 4282,2609
    holes={(2,1):0xC2E,(3,1):0xC2F,(7,2):0xC30}
    for dy in range(4):
        for dx in range(9):
            i=holes.get((dx,dy),(0xB77,0xB7B,0xB78)[dx%3] if dy==3 else 0xB79 if dx==8 and dy==0 else 0xB7A)
            r.add(i,2751 if (dx,dy) in holes else WOOD,4412+dx,2644+dy)
    for i,dx,dy in [(0xB5A,-1,2),(0xB58,9,2),(0xB59,1,-1),(0xC10,4,-1),(0xB59,7,-1),(0xB5B,1,4),(0xC11,4,4),(0xB5B,7,4)]:r.add(i,WOOD,4412+dx,2644+dy)
    for i,dx,dy in [(0x9F9,1,1),(0xA27,1,2),(0x9BF,1,3),(0x9D7,3,3),(0x991,5,1),(0x9F4,5,1),(0x9BF,5,3),(0xA27,6,1),(0x9BF,8,1),(0xFFB,3,0),(0x1E1C,6,3)]:r.surf(i,SETTING,4412+dx,2644+dy,-121)
    r.table(4424,2642,5,'wood',west=True,contents=((0x995,0),(0xFFD,1),(0xF01,2),(0x9A7,3),(0x998,4)))
    r.add(0x1AD6,ITEM,4426,2642);r.add(0x1AD7,ITEM,4426,2644);r.barrel(4426,2646,0xFAE);r.barrel(4426,2647,0x154D);r.add(0x1940,ITEM,4425,2648)
    r.chair(4423,2643,'e','wood');r.chair(4423,2645,'e','wood');r.skeleton(4422,2647,0x123F);r.over(0xF03,ITEM,4423,2647)
    r.small_table(4419,2651,contents=((0xFA7,),(0x999,)));r.chair(4418,2651,'e','wood');r.chair(4420,2651,'w','wood');r.add(0xC10,WOOD,4419,2652)
    r.add(0x3EDD,0,4413,2651);r.add(0x7B9D,0,4415,2653);r.add(0x7B9F,0,4411,2653);r.over(0xDCB,0,4410,2650);r.add(0x96D,ITEM,4426,2652);r.over(0x44CB,0,4425,2653)
    r.statue(4427,2641,0x4FBF);r.pool(4409,2648,life='octopus');r.kelp(4427,2654);r.kelp(4408,2654);r.moss(4421,2653);r.crab(4416,2652);r.fishes(4408,2642);r.brazier(4408,2640);r.brazier(4427,2649)
    r.sinkhole(4424,2650,0x30EE);r.over(0x1B4A,0,4409,2653)

    r=room('south','sunken_household')         # 4440-4454 x 2664-2670; north wall y=2663
    r.curtain(4441,2664,tall=False);r.north(0xC39,ITEM,4444,2664);r.painting(4448,2664,i=0xEA8);r.lamp(4446,2664);r.lamp(4452,2664,sconce=True);r.loom(4453,2664)
    r.add(0x975,ARMOR,4441,2665);r.table(4443,2666,4,'log',contents=((0x9E0,0),(0x9ED,1),(0x15F8,2),(0x103B,3)));r.add(0xA2A,WOOD,4444,2667);r.add(0xA2A,WOOD,4446,2667)
    r.add(0x1015,WOOD,4452,2667);r.add(0xA30,SHELF,4453,2668);r.add(0xC28,BONE,4450,2664);r.add(0xC27,BONE,4449,2665);r.add(0xC26,BONE,4448,2666)
    r.ornate(4441,2668,6,3,OLIVE);r.add(0x11FD,BONE,4442,2669);r.add(0x11FE,BONE,4443,2669);r.over(0x13A4,ITEM,4444,2669)
    r.add(0x24D7,ITEM,4440,2666);r.add(0xE44,ITEM,4440,2667);r.add(0x1046,ITEM,4440,2668);r.barrel(4440,2670,0x154D);r.add(0x10F2,ITEM,4447,2670);r.add(0x11CA,PLANT,4454,2670)
    r.over(0x9F3,ITEM,4442,2665);r.over(0x1E1D,0,4443,2665);r.over(0x1008,ITEM,4447,2667);r.skeleton(4449,2669);r.over(0x9C9,ITEM,4450,2669)
    r.mushrooms(4451,2667);r.crab(4451,2670,0x44D1);r.hay(4440,2669);r.moss(4445,2670,hue=RUG);r.sinkhole(4452,2670,0x30EA);r.candelabra(4441,2670)

    r=room('south','crypt_of_the_tide_lords')  # 4408-4422 x 2664-2684; north wall y=2663
    r.altar_disc(4410,2665);r.candelabra(4409,2664);r.candelabra(4413,2664,True);r.add(0x2FEF,SHELF,4409,2666);r.add(0x2FF0,SHELF,4413,2666)
    r.curtain(4409,2664);r.curtain(4415,2664);r.lamp(4411,2664,sconce=True);r.lamp(4417,2664,sconce=True);r.chains(4420,2664);r.tapestry(4421,2664,i=0xFDB)
    r.statue(4416,2665,0x12AE);r.statue(4419,2665,0x5074);r.statue(4421,2667,0x139F)
    r.mosaic(4414,2668,3,15,DARK)
    for x in (4410,4419):
        for y in (2669,2675):r.sarcophagus(x,y)
    r.tomb(4410,2680);r.tomb(4418,2680)
    for x,y in ((4412,2670),(4417,2670),(4412,2676),(4417,2676)):r.add(0x1178,TEAL,x,y)
    r.add(0x2FF9,TEAL,4413,2673);r.add(0x2FFA,TEAL,4416,2679);r.brazier(4409,2679);r.brazier(4421,2678);r.brazier(4415,2683,True)
    r.skulls(4408,2673);r.skulls(4422,2677);r.over(0x1AE1,BONE,4409,2683);r.bones(4421,2683,0xED0);r.bones(4411,2683,0xECD);r.urn(4408,2668,0x1AFC);r.add(0x241C,MARBLE,4422,2670);r.add(0x241D,MARBLE,4408,2677)
    r.pool(4416,2678,life='bones');r.crystal(4408,2683,0x61DD);r.glow(4422,2683);r.kelp(4408,2681);r.moss(4420,2673,hue=RUG);r.growth(4408,2670);r.crab(4413,2683,0x531F)
    r.sinkhole(4415,2673,0x30EA);r.cracks(4412,2679);r.column(4422,2681,True);r.over(0x1B4F,0,4416,2677)

    r=room('south','chapel_annex')             # 4400-4406 x 2672-2684; west wall 4399 except 2677
    r.tapestry(4400,2673,True,0xFD5);r.tapestry(4400,2680,True,0xFDD);r.lamp(4400,2675,True);r.lamp(4400,2682,True,True);r.curtain(4400,2678,True,False)
    r.statue(4403,2673,0x4FA7);r.statue(4406,2673,0x139D);r.bench(4401,2679);r.bench(4405,2679);r.candelabra(4403,2677,True)
    r.table(4402,2683,3,'stone',contents=((0x9BF,0,SETTING),(0x4FD6,1),(0x3031,2)));r.add(0x65D8,ITEM,4403,2675)
    r.pool(4401,2675,life='lily');r.moss(4405,2681);r.kelp(4406,2683);r.crab(4405,2676,0x44D1);r.skulls(4405,2683);r.sinkhole(4401,2682,0x30EE)

    r=room('south','shipwrights_dock')         # 4384-4406 x 2648-2670
    r.pool(4386,2650,True,jelly=0);r.pool(4385,2663,True,'fish',jelly=4)
    for y in range(2657,2663):
        for x in (4387,4388,4389,4390):r.over(0x5D1F if (x+y)%2 else 0x5D29,SEA,x,y)
    r.over(0xD0B,0,4388,2655);r.over(0xD08,0,4389,2661);r.fishes(4387,2659);r.over(0x540D,2814,4390,2664);r.over(0x79A7,0,4386,2655)
    r.deck(4392,2652,5,14)
    for y in range(2652,2666,2):r.add(0x880,PED,4391,y);r.add(0x880,PED,4391,y+1)
    r.add(0x93,PED,4391,2651);r.add(0x93,PED,4391,2666);r.add(0xA1F,ITEM,4396,2652);r.add(0xA1F,ITEM,4396,2665)
    r.add(0x229A,0,4389,2668);r.add(0x7B9C,0,4385,2668);r.add(0x7B9E,0,4390,2669);r.add(0x3E57,0,4394,2668);r.add(0x3EDD,0,4393,2667)
    r.over(0x14F8,0,4392,2653);r.add(0x14F9,0,4392,2656);r.add(0x4FB3,0,4392,2662);r.over(0x14FA,0,4393,2664);r.over(0xDC8,0,4394,2657);r.over(0xDD4,0,4395,2660);r.add(0x46B6,0,4396,2661);r.over(0x44CB,0,4395,2663)
    r.crates(4394,2653,2);r.add(0x96D,ITEM,4396,2656);r.add(0xAA6,ITEM,4396,2658);r.barrel(4395,2655,0xE77);r.barrel(4396,2655,0x154D);r.add(0x1EB5,ITEM,4394,2666);r.add(0x1DB4,ITEM,4395,2666)
    r.table(4398,2652,3,'log',contents=((0x14F1,0),(0x14F3,1),(0x13E4,2)));r.chair(4399,2653,'n','wood');r.over(0x1038,1881,4400,2653);r.add(0x19F1,WOOD,4402,2652)
    r.north(0x3D8F,0,4400,2648);r.north(0x3D8E,0,4404,2648);r.wheel(4402,2648);r.lamp(4401,2648);r.lamp(4406,2648,sconce=True);r.lantern(4403,2648);r.trophy(4405,2648,i=0x3383)
    r.crates(4402,2656,9);r.add(0x4BD0,ITEM,4405,2657);r.add(0x5082,ITEM,4405,2659);r.add(0x1039,ITEM,4402,2659);r.add(0x1C10,ITEM,4403,2659);r.add(0x1248,ITEM,4405,2661)
    r.add(0x11F3,WOOD,4405,2664);r.add(0x11F2,WOOD,4405,2665);r.skeleton(4402,2662,0x123F);r.over(0x1F26,ITEM,4403,2663);r.over(0xF02,ITEM,4401,2662);r.add(0xC11,WOOD,4401,2665)
    r.add(0x1AEE,BONE,4398,2668);r.add(0x3DC4,BONE,4400,2669);r.bones(4402,2669,0xECF);r.kelp(4384,2648);r.kelp(4406,2669);r.kelp(4384,2670,False);r.coral(4406,2651,0x6596);r.coral(4384,2660,0x6595)
    r.crab(4403,2666,0x65C0);r.crab(4399,2657);r.over(0x52C9,0,4385,2662);r.shells(4404,2669);r.moss(4399,2666);r.growth(4384,2656);r.sinkhole(4401,2668,0x30F2);r.cracks(4398,2661)

    r=room('south','infirmary')                # 4432-4454 x 2636-2646; west wall 4431 (gap 2639)
    for k in range(4):
        r.add(0x11FD,BONE,4450,2637+k*3);r.add(0x11FE,BONE,4451,2637+k*3);r.add(0xE7D,SHELF,4452,2637+k*3);r.over(0x1008,ITEM,4453,2637+k*3)
    r.surf(0x1236,BONE,4450,2640,-125);r.surf(0x123F,BONE,4451,2646,-125);r.over(0xE22,ITEM,4449,2638);r.over(0xE22,ITEM,4449,2644)
    r.curtain(4432,2637,True);r.curtain(4432,2643,True);r.lamp(4432,2638,True);r.lamp(4432,2645,True,True);r.painting(4432,2641,True,0x3DEC)
    r.ornate(4436,2639,8,5,OLIVE);r.table(4437,2641,5,'marble',contents=((0x1847,0,CRATE),(0xE2A,1),(0xF85,2),(0xE9B,3),(0x9A7,4)))
    r.chair(4438,2640,'s',hue=OLIVE);r.chair(4440,2640,'s',hue=OLIVE);r.add(0xC17,MARBLE,4439,2642);r.over(0xF9F,ITEM,4441,2642)
    r.add(0xB41,ITEM,4433,2636);r.add(0xB42,ITEM,4434,2636);r.add(0x2D0B,MARBLE,4436,2636);r.add(0xA34,SHELF,4438,2636);r.add(0xE44,ITEM,4440,2636);r.add(0xE48,ITEM,4441,2636);r.barrel(4443,2636,0x154D)
    r.add(0x26F5,ITEM,4445,2637);r.add(0x1AD6,ITEM,4446,2636);r.candelabra(4447,2636);r.brazier(4433,2646,True);r.brazier(4448,2646,True)
    r.add(0x11F3,WOOD,4435,2645);r.add(0x11F2,WOOD,4435,2646);r.over(0x13A4,ITEM,4437,2646);r.skeleton(4441,2646);r.over(0xF06,ITEM,4442,2646);r.over(0xF08,ITEM,4443,2645)
    r.moss(4443,2641);r.mushrooms(4445,2643);r.over(0xE20,ITEM,4444,2641);r.moss(4434,2642);r.kelp(4454,2636);r.kelp(4454,2646,False);r.crab(4446,2641,0x531F);r.sinkhole(4449,2646,0x30EA);r.cracks(4437,2638)

    r=room('south','tide_mages_laboratory')    # 4456-4470 x 2640-2654
    for k,i in enumerate((0x63F,0x640,0x641,0x642,0x643,0x644,0x645,0x646,0x647)):r.over(i,DARK,4462+k%3,2646+k//3)
    for x,y in ((4461,2645),(4465,2645),(4461,2649),(4465,2649)):r.add(0x1430,ITEM,x,y)
    for k,i in enumerate((0x4881,0x484A,0x4883,0x4881)):r.over(i,CRYS,(4460,4466,4460,4466)[k],(2647,2647,2650,2650)[k])
    r.add(0x1853,ITEM,4462,2650);r.over(0x1AE1,BONE,4464,2650)
    r.north(0x2DD3,WOOD,4457,2640);r.add(0x26F5,ITEM,4459,2641);r.add(0x3DB8,ARMOR,4461,2641);r.add(0x974,ARMOR,4467,2641);r.bookcase(4463,2640,dusty=True);r.bookcase(4464,2640,dusty=True);r.bookcase(4469,2640,vines=True)
    r.lamp(4461,2640);r.lamp(4466,2640,sconce=True);r.curtain(4468,2640);r.west(0x4FB5,ITEM,4456,2642,-114);r.lamp(4456,2646,True);r.curtain(4456,2641,True)
    r.table(4457,2643,3,'wood',contents=((0x4FD6,0),(0x1847,1,CRATE),(0xE9B,2)));r.chair(4458,2644,'n','wood');r.over(0x3B5A,ITEM,4457,2644)
    r.table(4468,2644,3,'wood',west=True,contents=((0xF07,0),(0xF0A,1),(0xF0C,2)));r.over(0xE4C,ITEM,4467,2645);r.over(0xF09,ITEM,4467,2646)
    r.add(0x178A,0,4469,2648);r.add(0x176C,0,4457,2653);r.crystal(4469,2652,0x61DA);r.glow(4458,2652);r.glow(4467,2653)
    r.table(4460,2652,3,'marble',contents=((0x65D4,0),(0x1E21,1),(0xE35,2)));r.chair(4461,2653,'n',hue=OLIVE);r.skeleton(4459,2653,0x123F);r.over(0xC16,ITEM,4464,2653)
    r.skulls(4468,2650);r.bones(4466,2653,0xECE);r.brazier(4457,2649,True);r.brazier(4469,2644,True);r.candelabra(4463,2653)
    r.mushrooms(4465,2651);r.moss(4457,2650,hue=RUG);r.kelp(4470,2654);r.crab(4458,2647,0x44D1);r.sinkhole(4469,2654,0x30EE)

    r=room('south','colonnade_of_the_drowned') # 4434-4460 x 2656-2662; north wall y=2655 (gap 4440-4446)
    for k,x in enumerate(range(4436,4459,4)):
        r.column(x,2657,broken=k%2==1);r.column(x,2661,broken=k%2==0)
        if k%2==0:r.lamppost(x+2,2659)
    r.ornate(4435,2658,25,3,SEA,holes={(9,1),(10,1),(17,2),(3,0)});r.banner(4448,2656,i=0x1588);r.banner(4452,2656,i=0x158A);r.lamp(4450,2656);r.lamp(4436,2656,sconce=True);r.tapestry(4438,2656,i=0xFD9)
    r.statue(4434,2657,0x31D1);r.statue(4460,2661,0x139E);r.statue(4434,2661,0x4FA7);r.statue(4460,2657,0x12A3)
    r.coral(4443,2657,0x6595);r.shells(4445,2661);r.leviathan(4449,2658);r.rubble(4455,2658);r.moss(4437,2660,hue=RUG);r.kelp(4459,2659);r.growth(4440,2661);r.crab(4446,2661,0x65C0);r.over(0x25F5,0,4441,2657)
    r.skulls(4457,2661);r.sinkhole(4453,2661,0x30F2);r.cracks(4445,2660);r.crystal(4458,2657,0x61E1);r.coral(4439,2657,0x6596)
    author_more_rooms(room)
    author_small_rooms(room)
    return rooms


def author_more_rooms(room):
    """Revision 4: every remaining room and corridor gets its own trade, story and life."""
    # ---------------------------------------------------------------- NORTH
    r=room('north','weavers_workshop')          # 4345-4359 x 2536-2542; walls north (y=2535, x<=4353) and west (x=4344)
    r.loom(4346,2537);r.add(0x1015,WOOD,4349,2538);r.add(0x1019,WOOD,4358,2540);r.add(0xFAB,ITEM,4352,2541);r.over(0xFA9,ITEM,4353,2541)
    r.table(4350,2539,4,'wood',contents=((0xF95,0),(0xF98,1),(0xF9B,2),(0x175D,3)));r.wood_bench(4351,2540);r.add(0xA2A,WOOD,4349,2540)
    r.over(0xE1D,ITEM,4355,2538);r.over(0xE1E,ITEM,4356,2538);r.over(0xE1F,ITEM,4355,2539);r.add(0x24D7,ITEM,4357,2537);r.surf(0xDF8,ITEM,4357,2537,-126);r.add(0x24D9,ITEM,4357,2538)
    r.over(0xFA1,ITEM,4348,2540);r.over(0xF9F,ITEM,4352,2540);r.over(0xF9D,ITEM,4354,2540);r.over(0x1760,ITEM,4355,2541);r.over(0x1762,ITEM,4356,2542);r.over(0x1766,ITEM,4348,2542)
    r.banner(4345,2536,i=0x1587);r.curtain(4347,2536);r.tapestry(4349,2536,i=0xFD9);r.lamp(4351,2536,sconce=True);r.add(0xA9D,SHELF,4345,2541);r.surf(0x1764,ITEM,4345,2541,-115);r.candelabra(4345,2539,True)
    r.skeleton(4347,2539);r.mushrooms(4357,2541);r.moss(4353,2537,hue=RUG);r.cracks(4351,2542);r.candelabra(4358,2537,True)

    r=room('north','chandlery')                 # 4321-4343 x 2536-2542; walls west (x=4320) and east (x=4344)
    r.west(0x3CC1,SHELF,4320,2537);r.surf(0x1430,ITEM,4320,2537,-115);r.west(0x3CC1,SHELF,4320,2538);r.surf(0xA0F,ITEM,4320,2538,-115);r.lamp(4320,2540,True,True);r.west(0x3CC4,SHELF,4320,2541);r.surf(0x1C18,ITEM,4320,2541,-115)
    r.table(4324,2538,4,'stone',contents=((0x1431,0),(0x1434,1),(0x142C,2),(0xA26,3)));r.table(4324,2541,4,'stone',contents=((0x1C19,0),(0x1C1B,1),(0x3543,2),(0x1C1D,3)))
    r.add(0x975,ARMOR,4330,2537);r.over(0x142A,ITEM,4331,2537);r.over(0x142B,ITEM,4331,2538);r.over(0x1422,ITEM,4330,2539);r.over(0x1425,ITEM,4332,2539);r.add(0x3BB8,ITEM,4333,2537)
    r.add(0xA1A,0,4335,2537);r.add(0xA1C,0,4336,2537);r.add(0xA1B,0,4335,2538);r.add(0xA22,0,4337,2538);r.add(0xA24,0,4338,2537);r.add(0xA1F,ITEM,4341,2537);r.add(0xA1F,ITEM,4341,2542)
    r.barrel(4340,2540,0x1097);r.barrel(4340,2541,0xE77);r.crate(4342,2540,0x4F8C,0x1433);r.crate(4342,2541,0x4F8F);r.add(0x1AD7,ITEM,4339,2542)
    r.chair(4325,2539,'n',hue=OLIVE);r.chair(4326,2542,'n',hue=OLIVE);r.skeleton(4327,2540,0x123F);r.over(0xA28,ITEM,4328,2540);r.candelabra(4334,2541);r.candelabra(4322,2542,True)
    r.moss(4336,2541,hue=RUG);r.cracks(4329,2542);r.crab(4338,2540);r.add(0x10EE,ITEM,4342,2542);r.glow(4334,2539);r.add(0x1AD6,ITEM,4342,2537)

    r=room('north','granary')                   # 4337-4358 x 2544-2549; north wall y=2543 for x<=4343, west wall x=4336
    r.north(0xC39,ITEM,4338,2544);r.lamp(4341,2544,sconce=True);r.lantern(4336,2545,True);r.west(0x3CC1,SHELF,4336,2548);r.surf(0x103B,ITEM,4336,2548,-115)
    for x,y in ((4339,2545),(4341,2545),(4339,2548),(4342,2548)):r.hay(x,y)
    r.add(0xF36,1881,4343,2546);r.add(0x100C,1881,4344,2545);r.add(0x100D,1881,4344,2546);r.add(0x880,PED,4346,2545);r.add(0x880,PED,4346,2546);r.add(0x880,PED,4346,2548);r.add(0x93,PED,4346,2547)
    for i,x,y in ((0x1039,4348,2545),(0x1045,4349,2545),(0x1046,4348,2546),(0x103A,4350,2546),(0x1248,4352,2545),(0x1C10,4353,2546),(0x1039,4351,2548),(0x1046,4352,2549)):r.add(i,ITEM,x,y)
    r.over(0x187E,ITEM,4349,2547);r.over(0x1880,ITEM,4350,2548);r.add(0x24D9,ITEM,4355,2545);r.add(0x65BB,ITEM,4356,2545);r.surf(0x9D0,ITEM,4356,2545,-122);r.barrel(4357,2546);r.barrel(4358,2546,0x1097)
    r.crates(4356,2548,5);r.over(0xE88,ITEM,4345,2549);r.over(0xF43,ITEM,4354,2549);r.add(0x10F2,ITEM,4358,2549)
    r.skeleton(4340,2547,0x123F);r.mushrooms(4342,2546);r.bones(4351,2549,0xECB);r.moss(4354,2547,hue=RUG);r.cracks(4347,2549);r.crab(4353,2544,0x44D1)

    r=room('north','brewery')                   # 4434-4455 x 2576-2582; walls west (x=4433), east (x=4456), north y=2575 (x 4434-4441, 4449-4452, 4454-4455)
    r.north(0xC3A,ITEM,4435,2576);r.lantern(4437,2576);r.lamp(4439,2576);r.banner(4449,2576,i=0x427);r.lamp(4452,2576,sconce=True);r.add(0xA9D,SHELF,4433,2580);r.surf(0x9C5,ITEM,4433,2580,-115)
    for k,x in enumerate(range(4436,4448,2)):r.barrel(x,2577,(0xFAE,0x1097,0xE77,0x154D,0x1EB5,0xFAE)[k]);r.barrel(x+1,2577,(0x1AD6,0x1AD7,0x1940,0x1AD6,0x1AD7,0x1940)[k])
    r.add(0x974,ARMOR,4453,2577);r.add(0x50AF,0,4454,2578);r.add(0x50AF,0,4454,2579);r.over(0x9D1,ITEM,4453,2579);r.over(0x1523,ITEM,4452,2578);r.barrel(4454,2580,0x154D);r.over(0x14E0,0,4452,2580)
    r.table(4438,2580,5,'log',contents=((0x9C4,0),(0x1F8D,1),(0x9C6,2),(0x1F9B,3),(0x998,4)));r.wood_bench(4439,2579);r.wood_bench(4441,2579);r.wood_bench(4438,2581,True);r.wood_bench(4442,2581,True)
    r.add(0x1DB4,ITEM,4447,2581);r.add(0x1DB5,ITEM,4448,2582);r.crate(4450,2581,0x4F96,0x9C7);r.crate(4451,2581,0x4F87);r.add(0xA2A,WOOD,4449,2579)
    r.skeleton(4445,2582,0x123F);r.over(0x9C7,ITEM,4446,2582);r.over(0x1F8F,ITEM,4444,2582);r.mushrooms(4435,2581);r.moss(4443,2578,hue=RUG);r.bones(4454,2582,0xECE);r.cracks(4448,2578);r.crab(4436,2582,0x531F)

    r=room('north','trophy_hall')               # 4441-4455 x 2544-2558; walls north (y=2543), west (x=4440), east (x=4456)
    r.wheel(4441,2544);r.trophy(4442,2544,i=0x337D);r.lamp(4444,2544,sconce=True);r.trophy(4445,2544,i=0x52C5);r.net(4447,2544);r.trophy(4448,2544,i=0x655C);r.lamp(4450,2544);r.trophy(4451,2544,i=0x3381);r.north(0x46BA,0,4453,2544,-116);r.trophy(4454,2544,i=0x52D6)
    r.ornate(4445,2548,7,5,OLIVE);r.table(4446,2550,4,'log',contents=((0x22AD,0,0),(0x52B1,1),(0xF62,2),(0xDD6,3,0)))
    r.statue(4443,2557,0x4578);r.statue(4453,2557,0x4579);r.statue(4453,2546,0x1AEF,BONE);r.statue(4448,2546,0x6596,0);r.statue(4444,2546,0x6595,0)
    r.add(0x3DC0,BONE,4450,2555);r.add(0x3DC8,BONE,4452,2554);r.over(0x25F5,0,4447,2556);r.wood_bench(4445,2553);r.wood_bench(4451,2553);r.brazier(4442,2558);r.brazier(4454,2558)
    r.add(0x77B3,0,4449,2556);r.add(0x77B9,0,4449,2552);r.crystal(4454,2555,0x61DA);r.bones(4444,2555,0xECF);r.skulls(4454,2548);r.cracks(4448,2557);r.moss(4442,2551,hue=RUG);r.urn(4454,2545,0x1AFE);r.add(0x3DBC,BONE,4441,2549);r.statue(4441,2554,0x25F5,0)

    r=room('north','counting_house')            # 4449-4463 x 2568-2574; walls west (x=4448), north (y=2567), east (x=4464)
    r.wallmap(4451,2568);r.north(0x1297,SHELF,4453,2568);r.bookcase(4455,2568,dusty=True);r.curtain(4457,2568);r.lamp(4459,2568,sconce=True);r.bookcase(4461,2568,dusty=True);r.lamp(4448,2570,True);r.west(0x3CC1,SHELF,4448,2572);r.surf(0x1E21,ITEM,4448,2572,-115)
    r.writing_table(4451,2570,((0x2936,),(0xFC0,)));r.chair(4451,2571,'n',hue=OLIVE);r.writing_table(4455,2570,((0x1851,),(0x1E20,)));r.chair(4455,2571,'n',hue=OLIVE);r.skeleton(4456,2571,0x123F)
    r.table(4458,2572,4,'marble',contents=((0x1BF7,0,ARMOR),(0x1BEB,1,0),(0x1BE5,2,ARMOR),(0xFEF,3)))
    r.add(0xE80,SHELF,4450,2574);r.add(0x9A8,ARMOR,4454,2574);r.chest(4452,2574,0xE40,ARMOR);r.chest(4453,2574,0xE41,ARMOR);r.chest(4462,2569,0x5080,MARBLE);r.chest(4462,2571,0x2823,MARBLE)
    r.over(0xEEE,0,4454,2573);r.over(0xEEA,0,4457,2569);r.over(0xEEC,0,4462,2573);r.over(0x1B45,0,4461,2574);r.candelabra(4462,2573);r.candelabra(4449,2569,True)
    r.moss(4460,2573,hue=RUG);r.cracks(4457,2574);r.glow(4461,2574);r.bones(4450,2569,0xECC);r.crab(4462,2568,0x44D1)

    r=room('north','collapsed_passage')         # 4377-4382 x 2554-2573; walls beyond reach on both sides
    for k,y in enumerate(range(2555,2572,4)):
        r.column(4377 if k%2 else 4382,y,broken=True);r.add((0x136B,0x136D,0x1363,0x1772)[k%4],ROCK,4381 if k%2 else 4378,y+1)
    r.add(0x278,ROCK,4379,2560);r.add(0x279,ROCK,4381,2565);r.sinkhole(4379,2558,0x30EA);r.sinkhole(4380,2566,0x30EE);r.sinkhole(4378,2570,0x30F2);r.cracks(4379,2562);r.cracks(4381,2569)
    r.over(0x1224,MARBLE,4380,2557);r.over(0x1226,MARBLE,4378,2564);r.crystal(4382,2560,0x61DA);r.crystal(4377,2568,0x61E1);r.glow(4380,2571);r.growth(4381,2562);r.coral(4377,2572,0x6597)
    r.skeleton(4379,2565,0x123F);r.bones(4381,2556,0xED0);r.skulls(4378,2573);r.mushrooms(4381,2572);r.kelp(4377,2554)

    r=room('north','rope_walk')                 # 4369-4391 x 2576-2582; walls west (x=4368), east (x=4392), north y=2575 (x 4369-4377, 4385-4391)
    r.net(4370,2576);r.lamp(4373,2576,sconce=True);r.wheel(4375,2576);r.lamp(4386,2576);r.net(4389,2576);r.lantern(4368,2578,True);r.west(0x3CC1,SHELF,4368,2581);r.surf(0xFA0,ITEM,4368,2581,-115)
    for k,x in enumerate(range(4371,4390,3)):r.add(0x93,PED,x,2579);r.over((0x14F8,0x14FA)[k%2],0,x+1,2579)
    r.add(0xF71,ITEM,4372,2577);r.add(0xF73,ITEM,4374,2577);r.add(0xF75,ITEM,4381,2577);r.add(0x3EDD,0,4384,2577);r.over(0x1F26,ITEM,4379,2578);r.over(0x1F26,ITEM,4383,2578)
    r.table(4375,2581,3,'log',contents=((0x1034,0),(0x102A,1),(0xFA1,2)));r.add(0xA2A,WOOD,4378,2581);r.add(0x898,0,4387,2581);r.over(0x44CB,0,4389,2582);r.add(0xAA6,ITEM,4390,2578)
    r.barrel(4380,2582,0x1097);r.barrel(4381,2582,0x1097);r.crates(4388,2578,8);r.add(0xA1F,ITEM,4370,2582);r.add(0xA1F,ITEM,4390,2582)
    r.skeleton(4377,2578,0x123F);r.over(0x14F8,0,4378,2577);r.crab(4385,2582,0x65C0);r.crab(4372,2580,0x44D1);r.shells(4385,2580);r.cracks(4387,2577);r.moss(4383,2581,hue=RUG)

    for half,x0 in (('west',4412),('east',4438)):
        r=room('north','avenue_of_lanterns_'+half)   # 4412-4460 x 2562-2565; open avenue, no walls in reach
        r.paving(x0,2563,23,1)
        for k,x in enumerate(range(x0+1,x0+23,4)):
            if k%2==0:r.lamppost(x,2562);r.lamppost(x,2565)
            else:r.column(x,2562,broken=k==3);r.add(0x11C9,PLANT,x,2565)
        if half=='west':
            r.statue(x0+7,2564,0x12AF);r.statue(x0+15,2564,0x144B);r.marble_bench(x0+3,2564);r.marble_bench(x0+19,2564);r.urn(x0+11,2564,0x1AFD)
            r.growth(x0+21,2564);r.sinkhole(x0+5,2565,0x30EA);r.crystal(x0+12,2565,0x61E1);r.over(0x25F5,0,x0+17,2563);r.cracks(x0+9,2562)
        else:
            r.statue(x0+7,2564,0x13A0);r.statue(x0+15,2564,0x144C);r.marble_bench(x0+3,2564);r.marble_bench(x0+19,2564);r.urn(x0+11,2564,0x1AFC)
            r.coral(x0+21,2564,0x6596);r.rubble(x0+16,2563);r.crystal(x0+12,2565,0x61DA);r.crab(x0+17,2563,0x531F);r.cracks(x0+9,2565);r.bones(x0+20,2562,0xECE)

    for half,x0 in (('west',4322),('east',4343)):
        r=room('north','drain_channel_'+half)   # 4321-4362 x 2576-2582; north wall y=2575, south wall y=2583
        for k in range(18 if half=='west' else 16):
            x=x0+k;r.over(0x5D1F if k%2 else 0x5D29,SEA,x,2579)
            if k%4==1:r.over(0x3A4,CRATE,x,2578);r.over(0x3A4,CRATE,x,2580)
        wx=(x0+2,x0+9,x0+5,x0+14,x0+16) if half=='west' else (4351,4356,4353,4358,4355)
        r.chains(wx[0],2576);r.chains(wx[1],2576);r.lamp(wx[2],2576,sconce=True);r.lamp(wx[3],2576,sconce=True);r.add(0x1B7D,BONE,wx[4],2576)
        r.over(0x540D,2817,x0+4,2579);r.over(0x540D,2966,x0+12,2579);r.over(0xDBA,PLANT,x0+8,2579);r.over(0x79A7,0,x0+15,2579)
        r.mushrooms(x0+1,2581);r.mushrooms(x0+11,2577);r.moss(x0+6,2581,hue=RUG);r.moss(x0+14,2581);r.add(0x10F2,ITEM,x0+3,2582);r.add(0x10EE,ITEM,x0+17,2581)
        r.skulls(x0+7,2582);r.bones(x0+13,2582,0xECD);r.bones(x0+9,2577,0xECA);r.crab(x0+10,2581,0x531F);r.sinkhole(x0+15,2582,0x30F2);r.add(0x1DB4,ITEM,x0+2,2577)
        if half=='east':r.chest(x0+15,2582,0x52E3,0);r.skeleton(x0+5,2582,0x123F)
        else:r.barrel(x0+15,2577,0x1DB5);r.skeleton(x0+12,2577)

    r=room('north','gallery_of_busts')          # 4425-4430 x 2545-2565; no reachable walls
    r.ornate(4427,2546,2,14,OLIVE)
    for k,y in enumerate(range(2546,2560,3)):
        r.statue(4425 if k%2 else 4430,y,(0x42BC,0x12CB,0x12CA,0x42BB,0x139A)[k]);r.painting(4424,y+1,True,(0x3321,0x3319,0x331E,0x3DEF,0x3DF4)[k])
        if k%2:r.marble_bench(4430,y+1,True)
        else:r.candelabra(4425,y+1,k%4==0)
    r.lamp(4424,2547,True);r.lamp(4424,2556,True,True);r.urn(4430,2545,0x1AFE);r.urn(4426,2559,0x1AFC);r.moss(4428,2559,hue=RUG);r.growth(4426,2554);r.crystal(4430,2549,0x61E1);r.cracks(4427,2555);r.crab(4429,2559,0x44D1);r.bones(4426,2551,0xECC)

    r=room('north','armorial_hall')             # 4456-4462 x 2545-2566; west wall x=4455 (except y=2551)
    for k,y in enumerate(range(2545,2559)):
        if y==2551:continue
        i=(0x156C,0x156D,0x156F,0x1575,0x157B,0x157F,0x1570,0x1571,0x1577,0x1579,0x156C,0x1575,0x157B,0x1571)[k]
        if k%3==2:r.lamp(4456,y,True,k%6==2)
        else:r.shield(4456,y,True,i)
    r.ornate(4458,2547,3,13,STONE)
    r.add(0x151A,ARMOR,4461,2546);r.add(0x151B,ARMOR,4461,2552);r.add(0x3D87,ARMOR,4461,2558);r.add(0xDC6,ARMOR,4459,2560);r.add(0x2842,ARMOR,4461,2549);r.add(0x2851,ARMOR,4461,2555)
    r.brazier(4462,2545);r.brazier(4462,2554,True);r.marble_bench(4462,2551,True);r.marble_bench(4462,2559,True)
    r.bones(4458,2560,0xED0);r.cracks(4460,2556);r.coral(4462,2560,0x6597);r.moss(4457,2545,hue=RUG);r.skulls(4462,2557)

    # -------------------------------------------------------------- CENTRAL
    r=room('central','votive_path')             # 4355-4360 x 2584-2605; north wall y=2583; the existing pool lies east
    r.tapestry(4354,2584,i=0xFD5);r.banner(4356,2584,i=0x1589);r.lamp(4358,2584,sconce=True)
    for k,y in enumerate(range(2586,2605,2)):
        r.over((0x1430,0x1431,0x1434,0xA0F,0xA10,0xA26,0xA28,0x142D,0x142E,0x142F)[k],ITEM,4360,y)
        if k%3==0:r.marble_bench(4358,y,True)
        if k%3==1:r.over((0xEEB,0xE97,0x3196,0x1B4A)[k%4],0,4359,y)
        if k%3==2:r.over((0xC3B,0xC3C,0xC3D,0xC3E)[k%4],ITEM,4359,y)
    r.add(0x3BEB,TEAL,4357,2594);r.surf(0x1B45,0,4357,2594,-125);r.candelabra(4356,2593);r.candelabra(4356,2595,True)
    r.statue(4355,2588,0x139D);r.statue(4355,2600,0x1226);r.urn(4355,2604,0x1AFE);r.urn(4355,2586,0x1AFC)
    for k,i in enumerate((0x4881,0x484A,0x4883)):r.over(i,CRYS,4358,2589+k*5)
    r.growth(4355,2596);r.moss(4356,2602,hue=RUG);r.crystal(4355,2591,0x61E1);r.glow(4359,2599);r.cracks(4357,2604)

    r=room('central','poolside_garden')         # 4371-4377 x 2592-2605; north wall y=2591 for x 4375-4377
    r.lamp(4376,2592);r.curtain(4377,2592,tall=False)
    r.paving(4373,2596,3,6);r.add(0x11C9,PLANT,4371,2593);r.add(0x11CA,PLANT,4377,2594);r.add(0x11C9,PLANT,4371,2604);r.add(0x11CA,PLANT,4377,2603)
    r.marble_bench(4372,2598);r.marble_bench(4376,2598);r.marble_bench(4372,2602);r.marble_bench(4376,2602);r.lamppost(4374,2593,True);r.lamppost(4374,2605,True)
    r.growth(4371,2596);r.growth(4376,2600);r.coral(4372,2600,0x6595);r.coral(4376,2596,0x6597);r.shells(4372,2604);r.over(0x25F5,0,4374,2600);r.crab(4375,2594);r.crystal(4371,2599,0x61DA)
    r.urn(4371,2601,0x1AFD);r.vase(4377,2599,0xB47);r.sinkhole(4373,2595,0x30EA);r.cracks(4375,2604)

    r=room('central','forecourt_of_the_tide_bell')   # 4361-4374 x 2584-2590; north wall y=2583 for x 4367-4374
    r.banner(4368,2584,i=0x1588);r.tapestry(4370,2584,i=0xFDB);r.banner(4372,2584,i=0x158F);r.lamp(4374,2584,sconce=True);r.lamp(4367,2584)
    r.paving(4365,2586,6,3);r.add(0x3BEB,TEAL,4367,2587);r.surf(0x1956,ITEM,4367,2587,-125)
    r.add(0x3BED,TEAL,4368,2587);r.surf(0x3196,0,4368,2587,-125);r.brazier(4364,2585);r.brazier(4372,2585)
    r.column(4362,2585);r.column(4373,2589,True);r.statue(4362,2589,0x12A2);r.statue(4374,2587,0x12A3)
    for k,i in enumerate((0x4881,0x484A,0x4883,0x4881)):r.over(i,CRYS,(4364,4371,4364,4371)[k],(2588,2588,2590,2590)[k])
    r.candelabra(4366,2589);r.candelabra(4369,2589,True);r.urn(4363,2590,0x1AFE);r.growth(4362,2587);r.crystal(4374,2590,0x61E1);r.crab(4370,2590,0x44D1);r.cracks(4363,2586);r.moss(4368,2589,hue=RUG)

    r=room('central','pottery')                 # 4320-4326 x 2593-2606; west wall x=4319
    for k,y in enumerate((2594,2595,2597,2598,2600,2601,2603,2604)):
        r.west(0x3CC1,SHELF,4320,y);r.surf((0xB48,0xB46,0x1AFC,0xB48,0x1AFD,0xB46,0x1AFE,0xB48)[k],(MARBLE,MARBLE,ITEM,MARBLE,ITEM,MARBLE,ITEM,MARBLE)[k],4320,y,-115)
    r.lamp(4320,2596,True);r.lamp(4320,2602,True,True);r.west(0xC39,ITEM,4320,2606)
    r.add(0x92F,ROCK,4325,2594);r.add(0xE7B,ITEM,4325,2596);r.add(0xA2A,WOOD,4323,2597);r.over(0x1008,ITEM,4322,2597);r.small_table(4322,2596,contents=((0xE44,),))
    for i,h,x,y in ((0x1AFC,ITEM,4322,2600),(0x1AFD,ITEM,4323,2600),(0x1AFE,ITEM,4324,2600),(0x241C,MARBLE,4325,2600),(0x241D,MARBLE,4322,2602),(0x398B,0,4323,2602),(0x39A2,0,4324,2602),(0x4B59,MARBLE,4325,2602),(0xB45,MARBLE,4322,2604),(0x42B2,MARBLE,4324,2604),(0x4042,MARBLE,4326,2604),(0x4043,MARBLE,4326,2601)):r.add(i,h,x,y)
    r.over(0xE48,ITEM,4326,2598);r.over(0xE4C,ITEM,4326,2599);r.over(0xE4A,ITEM,4321,2605);r.add(0xA9D,SHELF,4326,2595);r.crate(4326,2606,0x4F89,0xE45)
    r.skeleton(4323,2605);r.add(0xC2D,ITEM,4324,2606);r.moss(4321,2593,hue=RUG);r.cracks(4325,2605);r.crab(4321,2599,0x531F);r.over(0x79A7,0,4324,2598)

    r=room('central','wash_house')              # 4329-4335 x 2616-2630; north wall y=2615, west wall x=4328
    r.curtain(4330,2616);r.curtain(4333,2616);r.lamp(4334,2616,sconce=True);r.lamp(4328,2619,True);r.west(0xC39,ITEM,4328,2623);r.curtain(4328,2627,True,False)
    r.add(0xE7B,ITEM,4330,2618);r.add(0xE7B,ITEM,4334,2618);r.add(0xB41,ITEM,4331,2621);r.add(0xB42,ITEM,4332,2621);r.add(0x975,ARMOR,4334,2622);r.barrel(4330,2621,0x154D)
    r.table(4330,2625,4,'marble',contents=((0x175D,0),(0x1760,1),(0x1008,2),(0x1373,3)));r.add(0xA2A,WOOD,4330,2626);r.add(0x24D7,ITEM,4334,2626);r.surf(0x1762,ITEM,4334,2626,-126);r.add(0x24D9,ITEM,4334,2627)
    r.over(0x14E0,0,4333,2619);r.over(0x2004,0,4331,2624);r.over(0xFA1,ITEM,4332,2628);r.over(0x1764,ITEM,4331,2629);r.skeleton(4332,2623)
    for x,y in ((4331,2619),(4333,2622),(4330,2629),(4334,2624)):r.over(0x79A7,0,x,y)
    r.moss(4329,2629);r.moss(4333,2629,hue=RUG);r.crab(4334,2620,0x44D1);r.sinkhole(4333,2630,0x30EA);r.cracks(4330,2623)

    r=room('central','lecture_hall')            # 4329-4350 x 2593-2597; no reachable walls
    r.tapestry(4328,2594,True,0xFD5);r.lamp(4328,2596,True);r.paving(4329,2595,22,1);r.statue(4330,2594,0x65D4,ITEM);r.candelabra(4330,2593);r.candelabra(4330,2596,True);r.small_table(4331,2597,contents=((0x1810,),))
    for k,x in enumerate(range(4334,4350,3)):
        r.marble_bench(x,2593);r.marble_bench(x,2597);r.surf((0x1E20,0x3B59,0xFEF,0x1E21,0x3B54,0xFBD)[k],ITEM,x,2593,-126);r.surf((0xFC0,0xE34,0x1E20,0xEF4,0x3B5A,0xFBE)[k],ITEM,x,2597,-126)
        if k%2:r.marble_bench(x,2594);r.marble_bench(x,2596)
    r.skeleton(4341,2594);r.skeleton(4347,2596,0x123F);r.add(0x1047,ITEM,4350,2593);r.urn(4350,2597,0x1AFC)
    r.glow(4333,2596);r.cracks(4344,2595);r.moss(4349,2594,hue=RUG);r.crab(4336,2596,0x531F);r.crystal(4350,2595,0x61E1)

    # ----------------------------------------------------------------- EAST
    r=room('east','mosaic_gallery_west')        # 4433-4451 x 2584-2589; west wall x=4432
    r.tapestry(4432,2585,True);r.lamp(4432,2587,True);r.tapestry(4432,2589,True,0xEAC)
    for k,(x,hue) in enumerate(((4435,SEA),(4439,STONE),(4443,DARK),(4447,OLIVE))):
        r.mosaic(x,2585,3,3,hue,medallion=k%2==0);r.column(x+3,2584,broken=k==1);r.column(x+3,2589,broken=k==2)
    r.statue(4451,2584,0x31D0);r.statue(4451,2589,0x3F3D);r.marble_bench(4437,2588);r.marble_bench(4445,2588);r.urn(4434,2584,0x1AFE);r.urn(4451,2587,0x1AFC)
    r.growth(4449,2585);r.crystal(4441,2589,0x61DA);r.cracks(4436,2584);r.moss(4444,2589,hue=RUG);r.crab(4449,2588,0x44D1);r.shells(4448,2589)

    r=room('east','mosaic_gallery_east')        # 4454-4470 x 2584-2589; north wall y=2583 for x>=4457
    r.banner(4458,2584,i=0x1588);r.lamp(4460,2584,sconce=True);r.banner(4462,2584,i=0x158A);r.tapestry(4465,2584,i=0xFD7);r.lamp(4468,2584);r.banner(4470,2584,i=0x158F)
    r.ornate(4455,2585,4,3,RUG);r.mosaic(4461,2586,5,3,SEA,True);r.column(4459,2585);r.column(4459,2589,True);r.column(4466,2585,True);r.column(4466,2589)
    r.statue(4454,2589,0x31CB);r.statue(4470,2589,0x63C2);r.marble_bench(4457,2588);r.marble_bench(4463,2589)
    r.rubble(4468,2586);r.sinkhole(4469,2588,0x30EE);r.cracks(4467,2589);r.coral(4470,2586,0x6595);r.kelp(4470,2587);r.bones(4462,2589,0xECF);r.crystal(4455,2585,0x61E1);r.growth(4456,2586)

    r=room('east','stables')                    # 4464-4470 x 2600-2622; west wall x=4463 (except y=2612)
    r.north(0xC39,ITEM,4465,2600);r.lamp(4468,2600,sconce=True);r.lantern(4464,2602,True);r.lamp(4464,2609,True);r.west(0xC3A,ITEM,4464,2615);r.lamp(4464,2620,True,True)
    for y in (2603,2608,2613,2618):
        r.add(0x880,PED,4465,y);r.add(0x880,PED,4466,y);r.add(0x93,PED,4467,y);r.hay(4465,y+1);r.add(0xF36,1881,4468,y+1)
    r.add(0xB43,ITEM,4470,2604);r.add(0xB44,ITEM,4470,2605);r.add(0xB41,ITEM,4469,2614);r.add(0xB42,ITEM,4470,2614);r.barrel(4470,2610,0x154D);r.over(0x14E0,0,4469,2611)
    r.add(0xF38,ARMOR,4469,2601);r.over(0x1374,ARMOR,4470,2602);r.add(0x1376,ARMOR,4469,2621);r.add(0x1377,ARMOR,4470,2621);r.over(0xE88,ITEM,4464,2612)
    r.statue(4470,2607,0x4578);r.statue(4470,2617,0x4579);r.add(0x100C,1881,4466,2622);r.add(0x100D,1881,4467,2622)
    r.skeleton(4466,2609,0x123F);r.mushrooms(4468,2620);r.moss(4465,2606,hue=RUG);r.bones(4469,2616,0xECB);r.crab(4468,2611,0x531F);r.cracks(4466,2617);r.add(0x10EE,ITEM,4468,2606)

    r=room('east','memorial_wall')              # 4416-4422 x 2606-2630; west wall x=4415
    for k,y in enumerate(range(2606,2622,3)):
        if k%2==0:r.west((0x426,0x427,0x42A,0x42B)[k//2%4],SHELF,4416,y)
        else:r.lamp(4416,y,True,k%4==1)
        r.add(0x1178,TEAL,4418,y);r.add(0x1178,TEAL,4421,y+1);r.over((0x1430,0x1431,0x1434,0xA0F)[k%4],ITEM,4419,y);r.over((0xC3B,0xC3C,0xC3D,0xC3E)[k%4],ITEM,4420,y+1)
    r.add(0x3BEB,TEAL,4419,2613);r.surf(0x1853,ITEM,4419,2613,-125);r.statue(4422,2606,0x12AE);r.statue(4422,2620,0x5074);r.add(0x241C,MARBLE,4417,2622);r.add(0x241D,MARBLE,4422,2614)
    r.bones(4420,2617,0xECE);r.skulls(4417,2613);r.moss(4421,2615,hue=RUG);r.crystal(4422,2612,0x61E1);r.glow(4418,2620);r.cracks(4420,2609);r.growth(4416,2610)

    r=room('east','nursery')                    # 4408-4422 x 2624-2630; north wall y=2623 for x 4408-4416
    r.curtain(4409,2624);r.painting(4411,2624,i=0xEA4);r.lamp(4413,2624,sconce=True);r.painting(4415,2624,i=0xEC8)
    r.add(0x4214,SHELF,4418,2625);r.over(0x2AF9,ITEM,4419,2627);r.over(0x2D81,0,4416,2626);r.add(0x11FD,BONE,4409,2626);r.add(0x11FE,BONE,4410,2626);r.add(0x11FD,BONE,4409,2629);r.add(0x11FE,BONE,4410,2629)
    r.add(0xC28,BONE,4422,2625);r.add(0xC27,BONE,4421,2626);r.add(0xC26,BONE,4420,2627);r.over(0x13A4,ITEM,4411,2627);r.over(0x13A5,ITEM,4411,2630);r.over(0x1397,ITEM,4421,2629)
    r.ornate(4412,2626,5,4,OLIVE);r.small_table(4414,2628,contents=((0x9E9,),(0x9EB,)));r.add(0xC17,MARBLE,4413,2628);r.add(0xA2A,WOOD,4415,2628);r.chest(4408,2624,0xE43);r.add(0x11CA,PLANT,4422,2630)
    r.moss(4416,2629,hue=RUG);r.cracks(4412,2630);r.crab(4408,2630,0x44D1);r.glow(4417,2629);r.mushrooms(4419,2629)

    r=room('east','private_bath')               # 4425-4430 x 2616-2630; north wall y=2615, west wall x=4424
    r.curtain(4426,2616);r.lamp(4428,2616,sconce=True);r.curtain(4430,2616);r.west(0x4FB5,ITEM,4424,2619,-114);r.lamp(4424,2622,True);r.curtain(4424,2626,True,False)
    r.add(0x36AC,MARBLE,4428,2619);r.add(0x2D0B,MARBLE,4426,2618);r.add(0x30DF,MARBLE,4430,2618);r.add(0xE7B,ITEM,4430,2621);r.over(0x1008,ITEM,4426,2620)
    r.over(0x13A4,ITEM,4427,2621);r.over(0x13A5,ITEM,4429,2622);r.urn(4425,2624,0x1AFE);r.urn(4430,2624,0x1AFD);r.marble_bench(4427,2624);r.candelabra(4429,2624,True)
    r.pool(4425,2625,life='lily');r.add(0x11C9,PLANT,4430,2630);r.moss(4429,2628);r.crab(4430,2627,0x531F);r.over(0x79A7,0,4428,2620);r.cracks(4426,2630)

    r=room('east','priests_cell')               # 4456-4462 x 2616-2630; north wall y=2615, west wall x=4455 (y<=2623)
    r.tapestry(4456,2617,True,0xFD5);r.lamp(4456,2620,True);r.curtain(4456,2623,True,False);r.curtain(4458,2616);r.lamp(4461,2616,sconce=True)
    r.add(0x3BEB,TEAL,4459,2618);r.surf(0x1430,ITEM,4459,2618,-125);r.candelabra(4458,2619);r.candelabra(4460,2619,True);r.statue(4462,2617,0x139D);r.over(0x4881,CRYS,4459,2620)
    r.add(0x11FD,BONE,4457,2626);r.add(0x11FE,BONE,4458,2626);r.chest(4457,2628);r.statue(4461,2624,0x65D8,ITEM);r.add(0xA2A,WOOD,4460,2625);r.over(0x1E20,ITEM,4460,2627);r.urn(4462,2629,0x1AFC)
    r.skeleton(4459,2622);r.moss(4461,2626,hue=RUG);r.glow(4457,2630);r.crab(4461,2630,0x44D1);r.cracks(4459,2628)

    r=room('east','rubble_fall')                # 4401-4406 x 2621-2634; east wall x=4407
    for x,y,i in ((4402,2622,0x136B),(4405,2623,0x136D),(4401,2626,0x1363),(4404,2627,0x1772),(4403,2630,0x136B),(4406,2631,0x136D),(4402,2633,0x1363)):r.add(i,ROCK,x,y)
    r.add(0x278,ROCK,4405,2625);r.add(0x279,ROCK,4402,2629);r.column(4406,2621,True);r.column(4401,2633,True)
    for k,(x,y) in enumerate(((4403,2624),(4401,2628),(4405,2629),(4403,2632))):r.sinkhole(x,y,(0x30E7,0x30EA,0x30EE,0x30F2)[k])
    r.over(0x1225,MARBLE,4404,2622);r.over(0x1227,MARBLE,4402,2626);r.coral(4406,2627,0x6596);r.coral(4401,2631,0x6597);r.crystal(4406,2634,0x61DA);r.glow(4404,2634);r.kelp(4401,2622)
    r.skeleton(4404,2631,0x123F);r.bones(4402,2624,0xECF);r.chest(4405,2633,0x52E2,0);r.growth(4405,2621);r.cracks(4402,2631);r.cracks(4405,2626)

    r=room('east','antechamber')                # 4384-4406 x 2584-2589; north wall y=2583 (x 4384-4391, 4399-4406), west wall x=4383
    r.banner(4385,2584);r.shield(4387,2584,i=0x1577);r.lamp(4389,2584,sconce=True);r.weapons(4391,2584);r.weapons(4399,2584);r.lamp(4401,2584);r.shield(4403,2584,i=0x1571);r.banner(4405,2584,i=0x1589)
    r.tapestry(4384,2586,True);r.lamp(4384,2588,True)
    r.ornate(4394,2585,3,5,STONE);r.statue(4392,2585,0x12D8);r.statue(4398,2585,0x12D9);r.brazier(4393,2589);r.brazier(4397,2589)
    r.marble_bench(4386,2586,True);r.marble_bench(4386,2588,True);r.marble_bench(4404,2586,True);r.marble_bench(4404,2588,True)
    r.table(4388,2588,3,'stone',contents=((0xE35,0),(0x14ED,1),(0xFC0,2)));r.chair(4389,2587,'s',hue=OLIVE);r.lamppost(4402,2587);r.urn(4406,2589,0x1AFE);r.urn(4385,2589,0x1AFC)
    r.cracks(4400,2588);r.moss(4405,2585,hue=RUG);r.crystal(4406,2586,0x61E1);r.crab(4391,2589,0x44D1);r.bones(4400,2586,0xECC)

    r=room('east','cartwrights_yard')           # 4408-4430 x 2585-2589; west wall x=4407
    r.lantern(4408,2585,True);r.west(0xC39,ITEM,4408,2587);r.lamp(4408,2589,True)
    r.add(0x1BD8,ITEM,4410,2586);r.add(0x1BD9,ITEM,4411,2586);r.over(0x1BD7,ITEM,4412,2586);r.add(0x1BDE,ITEM,4410,2588);r.add(0x1BDF,ITEM,4411,2588);r.over(0x1BDD,ITEM,4412,2589);r.over(0x1BE0,ITEM,4409,2589)
    r.over(0x506E,0,4415,2586);r.over(0x506F,0,4416,2588);r.add(0x4C18,ITEM,4418,2586);r.add(0x19F1,WOOD,4421,2586);r.add(0x19F5,WOOD,4423,2586)
    r.table(4420,2588,4,'log',contents=((0x1034,0),(0x102A,1),(0x1028,2),(0x13E4,3)));r.add(0xA2A,WOOD,4424,2589);r.over(0x1038,1881,4419,2589);r.over(0x1038,1881,4425,2587)
    r.barrel(4427,2586,0x1097);r.barrel(4428,2586);r.crates(4428,2588,4);r.over(0x14F8,0,4426,2589);r.add(0x1EB5,ITEM,4426,2586)
    r.skeleton(4414,2588,0x123F);r.mushrooms(4413,2585);r.moss(4429,2585,hue=RUG);r.crab(4417,2589,0x531F);r.cracks(4422,2585);r.bones(4430,2589,0xECD)

    # ---------------------------------------------------------------- SOUTH
    r=room('south','embalmers_workshop')        # 4456-4470 x 2672-2683; north wall y=2671 for x 4463-4466, 4468-4470; west wall x=4455 for y<=2679
    r.curtain(4463,2672);r.lamp(4465,2672,sconce=True);r.chains(4468,2672);r.tapestry(4470,2672,i=0xFDB);r.tapestry(4456,2673,True,0xFDD);r.lamp(4456,2676,True);r.curtain(4456,2679,True,False)
    r.table(4460,2675,3,'stone',contents=((0xE21,0),(0x1236,1,BONE),(0x1373,2)));r.candelabra(4459,2674);r.candelabra(4463,2674,True)
    for i,x in ((0x2FEE,4458),(0x2FEF,4459),(0x2FF0,4460),(0x2FF1,4461)):r.add(i,SHELF,x,2679)
    r.table(4464,2678,3,'marble',west=True,contents=((0xE9B,0),(0x1851,1),(0x26F5,2)));r.add(0x975,ARMOR,4469,2674);r.over(0xEE9,ITEM,4468,2675);r.over(0xE21,ITEM,4467,2675)
    r.sarcophagus(4468,2677);r.urn(4457,2682,0x1AFE);r.urn(4470,2682,0x1AFD);r.add(0xE44,ITEM,4466,2682);r.add(0xE4C,ITEM,4467,2682)
    r.over(0x1853,ITEM,4462,2681);r.bones(4465,2681,0xECD);r.skulls(4456,2681);r.skeleton(4458,2677,0x123F);r.chest(4466,2680,0xE7E,SHELF)
    r.moss(4462,2682,hue=RUG);r.crystal(4470,2683,0x61DA);r.glow(4459,2683);r.crab(4464,2683,0x44D1);r.cracks(4461,2678);r.mushrooms(4457,2680)

    r=room('south','ossuary')                   # 4424-4430 x 2664-2683; north wall y=2663, west wall x=4423
    r.chains(4424,2666,True);r.lamp(4424,2669,True);r.tapestry(4424,2672,True,0xFD5);r.chains(4424,2674,True);r.lamp(4424,2678,True,True);r.candelabra(4430,2664,True)
    r.ornate(4426,2666,2,16,DARK)
    for k,y in enumerate(range(2665,2683,2)):
        r.add((0x1AD9,0x1ADC,0x1ADF)[k%3],BONE,4424 if k%2 else 4430,y);r.over((0x1AE0,0x1AE1,0x1AE2,0x1AE3)[k%4],BONE,4430 if k%2 else 4424,y+1)
        r.over((0xECA,0xECB,0xECC,0xECD,0xECE,0xECF,0xED0,0xED2,0x1B17)[k],BONE,4425+k%3,y)
    r.add(0x1C3C,TEAL,4428,2669);r.add(0x1C3E,TEAL,4428,2677);r.add(0x1B09,BONE,4429,2673);r.over(0x1B0E,BONE,4425,2679);r.over(0x1B1B,BONE,4429,2681)
    r.over(0x1853,ITEM,4426,2670);r.over(0x1857,ITEM,4427,2678);r.candelabra(4429,2666);r.brazier(4429,2683,True);r.urn(4424,2683,0x241C)
    r.mushrooms(4425,2682);r.moss(4429,2670,hue=RUG);r.crystal(4424,2680,0x61E1);r.glow(4430,2675)

    r=room('south','net_menders')               # 4409-4431 x 2656-2661; north wall y=2655
    r.net(4411,2656);r.lantern(4414,2656);r.lamp(4417,2656,sconce=True);r.net(4420,2656);r.wheel(4423,2656);r.lamp(4426,2656);r.net(4429,2656)
    for k,(x,y) in enumerate(((4410,2658),(4413,2660),(4416,2658),(4419,2660),(4422,2658),(4425,2660),(4428,2658))):r.over((0xDC9,0xDCA,0xDCB,0xDD0,0xDD1,0xDD3,0xDD5)[k],0,x,y)
    for x in (4412,4418,4424):r.add(0xA2A,WOOD,x,2659)
    r.over(0xDBF,0,4415,2657);r.over(0xDC0,0,4421,2657);r.over(0xDCC,0,4414,2659);r.over(0xDCE,0,4427,2659);r.over(0xFA0,ITEM,4417,2659);r.over(0xFA1,ITEM,4423,2659)
    r.add(0x2019,CRATE,4430,2659);r.crab(4430,2659,0x22A9);r.add(0x2019,CRATE,4430,2661);r.crab(4430,2661,0x65C0);r.add(0x24D7,ITEM,4409,2661);r.surf(0x9CC,SHELF,4409,2661,-126);r.add(0x24D9,ITEM,4410,2661)
    r.add(0x46B6,0,4426,2657);r.over(0x44CB,0,4427,2661);r.barrel(4409,2657,0x154D);r.over(0x14FA,0,4419,2661);r.skeleton(4413,2661,0x123F);r.over(0xDD2,0,4412,2661)
    r.shells(4416,2661);r.crab(4421,2661,0x44D1);r.fishes(4425,2661);r.moss(4428,2660,hue=RUG);r.cracks(4418,2661)

    r=room('south','salt_store')                # 4456-4462 x 2664-2670; north wall y=2663, west wall x=4455
    r.west(0x3CC1,SHELF,4456,2665);r.surf(0xE44,ITEM,4456,2665,-115);r.west(0x3CC1,SHELF,4456,2667);r.surf(0xE48,ITEM,4456,2667,-115);r.lamp(4456,2669,True);r.lamp(4456,2664,True,True);r.candelabra(4461,2664)
    r.add(0x55EF,0,4458,2666);r.add(0x55EF,0,4459,2666);r.add(0x55EF,0,4458,2667);r.over(0x2FE9,0,4460,2667);r.over(0x2FE9,0,4457,2669);r.add(0x1039,ITEM,4461,2666);r.add(0x1046,ITEM,4462,2667)
    r.barrel(4462,2665,0x1097);r.crate(4461,2669,0x4F92,0x1851);r.crystal(4462,2670,0x61DA);r.crystal(4457,2670,0x61DE);r.glow(4460,2669);r.cracks(4459,2668)

    r=room('south','reagent_store')             # 4464-4470 x 2656-2670; west wall x=4463
    for k,y in enumerate((2657,2659,2661,2663)):
        r.west(0x3CC4 if k%2 else 0x3CC1,SHELF,4464,y);r.surf((0xF06,0xF07,0xF0A,0xF0C)[k],ITEM,4464,y,-115)
    r.lamp(4464,2665,True);r.west(0xC39,ITEM,4464,2668)
    r.table(4466,2658,3,'marble',contents=((0x1851,0),(0xE9B,1),(0x26F5,2)));r.add(0x974,ARMOR,4470,2657);r.chair(4467,2659,'n',hue=OLIVE)
    r.over(0xC41,ITEM,4466,2661);r.over(0xC42,ITEM,4468,2661);r.over(0x18E3,ITEM,4467,2662);r.over(0x18E5,ITEM,4469,2662);r.over(0x18DD,ITEM,4466,2663);r.over(0x18EB,ITEM,4468,2663);r.over(0xF7B,ITEM,4470,2663)
    r.add(0x222E,PLANT,4466,2666);r.add(0x222F,PLANT,4469,2667);r.mushrooms(4467,2668);r.mushrooms(4465,2669);r.over(0xD17,PLANT,4470,2670);r.over(0xD19,PLANT,4468,2670)
    r.crate(4470,2665,0x4F8D,0x1847);r.barrel(4470,2660,0x1097);r.add(0xE44,ITEM,4465,2665);r.add(0xE4C,ITEM,4469,2664)
    r.skeleton(4466,2665,0x123F);r.glow(4470,2668);r.crystal(4465,2667,0x61DE);r.moss(4468,2665,hue=RUG);r.crab(4466,2670,0x531F);r.cracks(4469,2659)



def author_small_rooms(room):
    """Revision 4: the remaining cells, corridors and nooks, each with its own small story."""
    # ---------------------------------------------------------------- NORTH
    r=room('north','minstrels_gallery')         # 4360-4372 x 2536-2541; north wall x<=4367, west wall x=4359
    r.tapestry(4362,2536,i=0xFD7);r.banner(4365,2536);r.lamp(4367,2536,sconce=True);r.lamp(4360,2538,True);r.curtain(4360,2540,True,False)
    for k,x in enumerate((4363,4365,4367,4369)):r.add((0xEB5,0xEB6,0xEB7,0xEB8)[k],WOOD,x,2538);r.add((0xEBD,0xEBE,0xEBF,0xEC0)[k],ITEM,x,2537)
    r.add(0xEB1,ITEM,4371,2539);r.over(0xEB3,ITEM,4361,2541);r.over(0xE9A,ITEM,4372,2541);r.over(0x194B,ITEM,4368,2540)
    r.wood_bench(4363,2540);r.wood_bench(4365,2540);r.wood_bench(4367,2540);r.skeleton(4362,2539);r.candelabra(4372,2537)
    r.moss(4369,2541,hue=RUG);r.crystal(4361,2537,0x61E1);r.cracks(4366,2541);r.crab(4370,2536,0x44D1)

    r=room('north','pantry_passage')            # 4392-4397 x 2537-2549; north wall y=2536, west wall x=4391 (y 2543-2546, 2548-2549)
    r.add(0xA1F,ITEM,4394,2537);r.candelabra(4396,2537);r.west(0x3CC1,SHELF,4392,2543);r.surf(0x97E,ITEM,4392,2543,-115);r.west(0x3CC1,SHELF,4392,2545);r.surf(0x9C9,ITEM,4392,2545,-115);r.west(0x3CC4,SHELF,4392,2548);r.surf(0x9C0,ITEM,4392,2548,-115);r.lantern(4392,2544,True)
    r.crate(4396,2539,0x4F8A,0x97C);r.crate(4396,2540,0x4F8E,0x9D0);r.add(0x1039,ITEM,4393,2539);r.add(0x1046,ITEM,4393,2540);r.add(0x24D7,ITEM,4397,2542);r.surf(0x103B,ITEM,4397,2542,-126);r.add(0x65BB,ITEM,4397,2543)
    r.barrel(4396,2546,0x154D);r.barrel(4396,2547);r.over(0x97E,ITEM,4394,2542);r.over(0x18E3,ITEM,4394,2544);r.over(0x9C9,ITEM,4395,2545);r.over(0xF02,ITEM,4394,2547);r.add(0x10F2,ITEM,4397,2549)
    r.skeleton(4394,2548,0x123F);r.mushrooms(4393,2541);r.moss(4395,2549,hue=RUG);r.crab(4393,2549);r.cracks(4395,2538)

    r=room('north','vestry')                    # 4352-4366 x 2529-2533; north wall only at x 4360, 4364; west wall x=4351
    r.curtain(4360,2529);r.banner(4364,2529);r.tapestry(4352,2530,True,0xFD5);r.lamp(4352,2532,True)
    r.chest(4354,2530,0x2821,MARBLE);r.chest(4355,2530,0x2823,MARBLE);r.chest(4353,2533,0x280B,MARBLE);r.table(4357,2531,4,'marble',contents=((0x175D,0),(0x1760,1),(0x6395,2,MARBLE),(0x1430,3)))
    r.over(0x1762,ITEM,4361,2533);r.over(0x1764,ITEM,4363,2530);r.add(0xC17,MARBLE,4365,2531);r.candelabra(4362,2529);r.candelabra(4366,2533,True);r.urn(4366,2529,0x1AFE)
    r.over(0x4881,CRYS,4359,2533);r.moss(4364,2532,hue=RUG);r.glow(4356,2533);r.cracks(4360,2531)

    r=room('north','root_cellar')               # 4319-4326 x 2544-2549; north wall y=2543
    r.north(0xC39,ITEM,4320,2544);r.lamp(4323,2544,sconce=True);r.curtain(4325,2544,tall=False)
    r.add(0x1039,ITEM,4320,2546);r.add(0x1045,ITEM,4321,2546);r.add(0x24D7,ITEM,4322,2547);r.surf(0x9D0,ITEM,4322,2547,-126);r.add(0x24D9,ITEM,4323,2547);r.surf(0x9D1,ITEM,4323,2547,-126);r.add(0x65BB,ITEM,4325,2546)
    r.barrel(4326,2548,0x154D);r.barrel(4325,2549);r.crate(4320,2549,0x4F92,0xC61);r.over(0x18E3,ITEM,4324,2548);r.over(0xE44,ITEM,4321,2549);r.over(0x9D2,ITEM,4324,2545)
    r.mushrooms(4319,2547);r.moss(4321,2544,hue=RUG);r.bones(4326,2545,0xECB);r.crab(4319,2549,0x531F)

    r=room('north','smokehouse')                # 4328-4334 x 2544-2549; west wall x=4327
    for k,y in enumerate((2544,2546,2548)):r.west(0x3CC1,SHELF,4328,y);r.surf((0x9C9,0x9CC,0x9C0)[k],(ITEM,SHELF,ITEM)[k],4328,y,-115)
    r.lamp(4328,2545,True,True);r.brazier(4331,2546,True);r.add(0x975,ARMOR,4333,2545);r.barrel(4333,2548,0x154D);r.barrel(4334,2549);r.over(0x9CE,SHELF,4330,2548);r.over(0x1E1C,0,4331,2549);r.over(0x9C9,ITEM,4332,2547)
    r.add(0x24D7,ITEM,4330,2544);r.surf(0x9CD,SHELF,4330,2544,-126);r.over(0x10EE,ITEM,4334,2547);r.crab(4329,2549,0x44D1);r.moss(4333,2544,hue=RUG);r.cracks(4331,2548)

    r=room('north','cooperage')                 # 4432-4438 x 2568-2573; north wall y=2567
    r.north(0xC39,ITEM,4433,2568);r.lamp(4436,2568,sconce=True);r.lantern(4438,2568)
    r.add(0x1EB5,ITEM,4433,2570);r.add(0x1EB5,ITEM,4434,2571);r.add(0x1DB4,ITEM,4432,2572);r.add(0x19F1,WOOD,4436,2570);r.over(0x1034,ITEM,4437,2570);r.over(0x102A,ITEM,4436,2571);r.over(0x1038,1881,4435,2572)
    r.barrel(4438,2572,0xFAE);r.barrel(4438,2573,0x1097);r.add(0x1BD8,ITEM,4432,2569);r.over(0x1BD7,ITEM,4433,2573);r.add(0xA2A,WOOD,4437,2572)
    r.skeleton(4435,2569,0x123F);r.mushrooms(4436,2573);r.cracks(4434,2569);r.crab(4432,2573,0x531F)

    r=room('north','pitch_store')               # 4440-4446 x 2568-2573; west wall x=4439
    r.lantern(4440,2569,True);r.west(0xC3A,ITEM,4440,2572);r.add(0x975,ARMOR,4442,2569);r.barrel(4445,2569,0x1097);r.barrel(4446,2569,0x1097);r.barrel(4446,2570,0x1097)
    r.over(0x14E0,0,4443,2570);r.over(0x1373,ITEM,4441,2571);r.over(0x14F8,0,4444,2572);r.add(0x7B9D,0,4441,2573);r.add(0x7B9F,0,4445,2573);r.over(0x1038,1881,4443,2573)
    r.moss(4444,2571);r.bones(4446,2573,0xECE);r.crab(4440,2573,0x44D1);r.sinkhole(4442,2572,0x30EA)

    r=room('north','sentry_corridor')           # 4345-4350 x 2552-2573; walls at y=2559 and y=2567 split it into three cells
    r.brazier(4346,2553,True);r.stone_bench(4349,2554);r.skeleton(4347,2556,0x123F);r.over(0xF62,ITEM,4348,2556);r.over(0xFA7,ITEM,4349,2557);r.moss(4345,2557,hue=RUG);r.cracks(4348,2553)
    r.banner(4346,2560,i=0x1588);r.lamp(4348,2560,sconce=True);r.banner(4350,2560,i=0x158A);r.stone_bench(4345,2563,True);r.urn(4350,2565,0x1AFE);r.crystal(4346,2566,0x61E1);r.growth(4348,2564)
    r.tapestry(4346,2568,i=0xFDB);r.lamp(4349,2568);r.statue(4350,2571,0x12A0);r.skulls(4345,2572);r.mushrooms(4347,2571);r.sinkhole(4348,2573,0x30EE)

    r=room('north','bath_vestibule')            # 4392-4397 x 2576-2582; west wall x=4391
    r.curtain(4392,2577,True);r.lamp(4392,2579,True);r.curtain(4392,2581,True,False)
    r.add(0x2D0B,MARBLE,4394,2577);r.add(0xE7B,ITEM,4397,2577);r.marble_bench(4394,2580);r.marble_bench(4396,2580);r.over(0x13A4,ITEM,4395,2580);r.over(0x13A5,ITEM,4397,2581);r.urn(4397,2582,0x1AFD);r.over(0x1008,ITEM,4395,2578)
    r.over(0x79A7,0,4394,2582);r.moss(4393,2581);r.crab(4396,2578,0x531F);r.cracks(4395,2579)

    r=room('north','oubliette')                 # 4457-4462 x 2576-2582 dead end; west wall x=4456
    r.chains(4456,2577,True);r.lamp(4456,2579,True,True);r.chains(4456,2581,True)
    r.add(0x570F,CRATE,4460,2577);r.add(0x570F,CRATE,4460,2580);r.skeleton(4461,2578);r.skeleton(4461,2581,0x123F);r.skulls(4462,2579);r.over(0x3A4,CRATE,4459,2579);r.bones(4458,2580,0xECD);r.over(0x1AE1,BONE,4462,2577)
    r.brazier(4462,2581,True);r.mushrooms(4458,2580);r.moss(4459,2577,hue=RUG);r.add(0x10EE,ITEM,4458,2578)

    r=room('north','stair_niche')               # 4360-4365 x 2542-2549; west wall x=4359 (except y=2543)
    r.tapestry(4360,2545,True,0xFD9);r.lamp(4360,2548,True);r.statue(4364,2544,0x12D6);r.urn(4365,2549,0x1AFC);r.marble_bench(4362,2547,True)
    r.moss(4361,2548,hue=RUG);r.crystal(4365,2546,0x61DA);r.cracks(4363,2549);r.growth(4362,2544)

    r=room('north','east_gate_niche')           # 4465-4470 x 2545-2550
    r.statue(4467,2546,0x12D6);r.brazier(4465,2549,True);r.brazier(4470,2549,True);r.coral(4470,2545,0x6596);r.growth(4465,2545);r.cracks(4468,2549);r.shells(4468,2548);r.urn(4470,2547,0x1AFE)

    # -------------------------------------------------------------- CENTRAL
    r=room('central','timekeepers_alcove')      # 4337-4349 x 2625-2630
    r.table(4340,2627,4,'marble',contents=((0x1810,0),(0x1812,1),(0x1814,2),(0x1816,3)));r.add(0x44D5,SHELF,4346,2626);r.add(0xC1F,ITEM,4348,2628);r.candelabra(4339,2626);r.candelabra(4345,2629,True)
    r.marble_bench(4341,2629);r.marble_bench(4343,2629);r.urn(4337,2630,0x1AFE);r.over(0x1811,ITEM,4347,2628);r.skeleton(4344,2630)
    r.sinkhole(4338,2628,0x30EA);r.glow(4349,2626);r.moss(4347,2629,hue=RUG);r.crab(4338,2626,0x44D1);r.cracks(4342,2625)

    r=room('central','scroll_store')            # 4344-4357 x 2609-2613; west wall x=4343
    for k,y in enumerate((2609,2611,2613)):r.west(0x3CC1,SHELF,4344,y);r.surf((0xE35,0xEF4,0x1F2D)[k],ITEM,4344,y,-115)
    r.lamp(4344,2610,True);r.lamp(4344,2612,True,True)
    r.crate(4347,2610,0x4F8B,0xE37);r.crate(4347,2611,0x4F8F,0xEF6);r.add(0xC13,SHELF,4351,2609);r.add(0x89A,WOOD,4354,2610);r.add(0xA2A,WOOD,4349,2612)
    for k,(x,y) in enumerate(((4346,2613),(4349,2609),(4352,2612),(4355,2613),(4356,2610),(4350,2611))):r.over((0xE34,0xE36,0xE38,0xEF3,0xEF7,0xC16)[k],ITEM,x,y)
    r.add(0x1430,ITEM,4353,2611);r.skeleton(4348,2613,0x123F);r.sinkhole(4356,2612,0x30EE);r.mushrooms(4357,2609);r.moss(4352,2613,hue=RUG)

    r=room('central','well_of_tides')           # 4320-4329 x 2609-2613
    r.add(0x65CE,TEAL,4324,2611);r.over(0x14E0,0,4325,2612);r.over(0x2004,0,4323,2612);r.over(0x14F8,0,4325,2610);r.add(0xB41,ITEM,4327,2610);r.add(0xB42,ITEM,4328,2610)
    r.add(0x11C9,PLANT,4320,2609);r.add(0x11CA,PLANT,4329,2613);r.marble_bench(4321,2612);r.marble_bench(4327,2613);r.lamppost(4320,2613,True);r.urn(4329,2609,0x1AFD)
    r.over(0x79A7,0,4324,2613);r.growth(4321,2610);r.crab(4328,2612,0x531F);r.cracks(4326,2609);r.crystal(4322,2609,0x61E1)

    r=room('central','lantern_corner')          # 4334-4342 x 2609-2613
    r.lamppost(4335,2610);r.lamppost(4341,2612);r.statue(4338,2610,0x12A1);r.urn(4334,2613,0x1AFC);r.urn(4342,2609,0x1AFE);r.marble_bench(4339,2613)
    r.moss(4336,2612);r.coral(4341,2609,0x6597);r.cracks(4337,2613);r.crab(4340,2611,0x44D1);r.glow(4334,2610)

    # ----------------------------------------------------------------- EAST
    r=room('east','bindery')                    # 4424-4430 x 2592-2606; west wall x=4423
    for k,y in enumerate((2593,2595,2597,2603,2605)):r.west(0x3CC1 if k%2 else 0x3CC4,SHELF,4424,y);r.surf((0x1E20,0xFBD,0x3B59,0x1E21,0xFEF)[k],ITEM,4424,y,-115)
    r.lamp(4424,2599,True);r.curtain(4424,2601,True,False)
    r.writing_table(4427,2594,((0xFC0,),(0x2051,)));r.chair(4427,2595,'n','wood');r.table(4426,2599,3,'wood',contents=((0x1E21,0),(0xFA1,1),(0xF9F,2)))
    r.over(0x1E20,ITEM,4429,2596);r.over(0xC16,ITEM,4426,2602);r.over(0x0F9D,ITEM,4429,2598);r.over(0xFA0,ITEM,4430,2600);r.add(0x1430,ITEM,4430,2603);r.add(0xA2A,WOOD,4428,2604);r.crate(4430,2606,0x4F87,0x1E21)
    r.skeleton(4426,2605);r.moss(4429,2593,hue=RUG);r.glow(4430,2593);r.cracks(4428,2602);r.crab(4425,2606,0x531F)

    r=room('east','illuminators_studio')        # 4432-4438 x 2593-2597; north wall y=2591
    r.north(0xEA0,0,4432,2592);r.north(0xEA4,0,4434,2592);r.lamp(4436,2592,sconce=True);r.north(0xEC8,0,4438,2592)
    r.add(0xF66,WOOD,4433,2594);r.add(0xF65,WOOD,4437,2594);r.small_table(4435,2595,contents=((0xFC1,),(0x1372,)));r.add(0xA2A,WOOD,4435,2596);r.over(0x1370,ITEM,4434,2596);r.over(0x1371,ITEM,4437,2596)
    r.add(0x1430,ITEM,4438,2597);r.over(0xEF4,ITEM,4432,2597);r.skeleton(4436,2597,0x123F);r.moss(4432,2595,hue=RUG);r.cracks(4438,2595)

    r=room('east','vault_niche')                # 4465-4470 x 2592-2597; west wall x=4463 except the treasury door at y=2595
    r.lamp(4464,2593,True,True);r.lamp(4464,2597,True);r.statue(4469,2593,0x144B);r.brazier(4466,2592,True);r.brazier(4466,2597,True);r.add(0x176C,0,4470,2596)
    r.over(0xEEE,0,4467,2594);r.over(0xEEB,0,4469,2596);r.chest(4470,2592,0x52E3,0);r.moss(4467,2596,hue=RUG);r.cracks(4468,2595);r.glow(4465,2596)

    r=room('east','lamp_niche')                 # 4456-4461 x 2601-2606; west wall x=4455
    r.tapestry(4456,2602,True,0xFDD);r.lamp(4456,2605,True);r.lamppost(4460,2602,True);r.marble_bench(4458,2604,True);r.urn(4461,2605,0x1AFE);r.vase(4461,2601,0xB47)
    r.moss(4457,2604);r.crystal(4459,2605,0x61DA);r.cracks(4460,2604);r.crab(4458,2602,0x44D1)

    r=room('east','shrine_niche')               # 4456-4461 x 2608-2613; north wall y=2607
    r.banner(4457,2608,i=0x1589);r.lamp(4459,2608,sconce=True);r.tapestry(4461,2608,i=0xFD5);r.add(0x3BED,TEAL,4459,2610);r.surf(0x1B45,0,4459,2610,-125);r.candelabra(4457,2611);r.candelabra(4461,2611,True)
    r.statue(4457,2613,0x139D);r.over(0x484A,CRYS,4459,2612);r.over(0xC3B,ITEM,4458,2610);r.over(0xC3D,ITEM,4460,2610);r.moss(4460,2612,hue=RUG);r.glow(4458,2609)

    r=room('east','linen_store')                # 4440-4446 x 2626-2630; west wall x=4439
    for k,y in enumerate((2626,2628,2630)):r.west(0x3CC1,SHELF,4440,y);r.surf((0x175D,0x1760,0x1764)[k],ITEM,4440,y,-115)
    r.lamp(4440,2627,True);r.curtain(4440,2629,True,False)
    r.add(0x24D7,ITEM,4443,2627);r.surf(0x1762,ITEM,4443,2627,-126);r.add(0x24D9,ITEM,4444,2627);r.chest(4446,2626,0xE43);r.chest(4446,2627);r.over(0x1766,ITEM,4442,2629);r.over(0xE1F,ITEM,4445,2629);r.add(0xA2A,WOOD,4442,2630)
    r.mushrooms(4445,2630);r.moss(4441,2626,hue=RUG);r.crab(4444,2630,0x531F)

    r=room('east','servants_quarters')          # 4448-4453 x 2626-2630; west wall x=4447
    r.lamp(4448,2627,True);r.west(0xC39,ITEM,4448,2629)
    r.add(0x11FD,BONE,4450,2626);r.add(0x11FE,BONE,4451,2626);r.add(0x11FD,BONE,4450,2629);r.add(0x11FE,BONE,4451,2629);r.surf(0x123F,BONE,4451,2629,-125);r.chest(4453,2627,0xE7D,SHELF);r.add(0xA2A,WOOD,4453,2629);r.add(0x1430,ITEM,4453,2630)
    r.over(0x13A4,ITEM,4449,2628);r.over(0xF02,ITEM,4452,2628);r.moss(4448,2627,hue=RUG);r.cracks(4452,2626);r.crab(4449,2630,0x44D1)

    # ---------------------------------------------------------------- SOUTH
    r=room('south','well_court')                # 4440-4453 x 2674-2678; west wall x=4439
    r.lantern(4440,2675,True);r.curtain(4440,2677,True,False)
    r.add(0x65CE,TEAL,4446,2676);r.over(0x14E0,0,4447,2677);r.over(0x2004,0,4445,2677);r.over(0x14F8,0,4447,2675);r.add(0xB41,ITEM,4449,2677);r.add(0xB42,ITEM,4450,2677)
    r.add(0x11C9,PLANT,4441,2678);r.add(0x11C9,PLANT,4452,2674);r.marble_bench(4443,2675);r.marble_bench(4451,2675);r.urn(4453,2678,0x1AFD);r.lamppost(4442,2674,True)
    r.over(0x79A7,0,4446,2678);r.growth(4448,2674);r.crab(4444,2678,0x531F);r.cracks(4452,2676);r.crystal(4453,2676,0x61E1)

    r=room('south','harbour_steps')             # 4380-4390 x 2640-2645; north wall y=2639 for x>=4383
    r.net(4384,2640);r.lamp(4386,2640,sconce=True);r.net(4389,2640);r.add(0xA1F,ITEM,4381,2640)
    r.add(0x3E57,0,4382,2642);r.add(0x7B9C,0,4380,2644);r.over(0x14FA,0,4384,2642);r.add(0x46B6,0,4388,2642);r.add(0x96D,ITEM,4390,2642);r.crates(4389,2644,6);r.barrel(4386,2644,0x154D)
    r.add(0x2019,CRATE,4383,2645);r.crab(4383,2645,0x22A9);r.over(0xDBF,0,4385,2643);r.shells(4380,2641);r.kelp(4380,2643);r.sinkhole(4387,2645,0x30EA);r.fishes(4386,2642)

    r=room('south','wreck_scatter')             # 4380-4390 x 2673-2678
    r.add(0x7B9C,0,4381,2674);r.add(0x7B9D,0,4384,2677);r.add(0x7B9E,0,4388,2674);r.add(0x7B9F,0,4386,2675);r.add(0x3EDD,0,4383,2675);r.add(0xE6E,ITEM,4389,2677)
    r.add(0x1DB4,ITEM,4380,2677);r.add(0x1DB5,ITEM,4390,2673);r.chest(4387,2678,0x52E2,0);r.bones(4382,2678,0xECF);r.over(0x1AE2,BONE,4385,2673);r.skeleton(4386,2677,0x123F)
    r.coral(4380,2673,0x6596);r.coral(4390,2675,0x6597);r.kelp(4384,2673);r.growth(4388,2677);r.sinkhole(4382,2676,0x30EE);r.sinkhole(4389,2676,0x30F2);r.crab(4385,2678,0x65C0);r.crystal(4387,2673,0x61DA)

    r=room('south','chapel_porch')              # 4392-4397 x 2673-2683; west wall x=4391 (y<=2679)
    r.tapestry(4392,2674,True,0xFD7);r.lamp(4392,2677,True);r.curtain(4392,2679,True,False)
    r.marble_bench(4394,2675);r.marble_bench(4396,2675);r.marble_bench(4394,2681);r.marble_bench(4396,2681);r.statue(4397,2673,0x12A0);r.candelabra(4393,2678);r.candelabra(4397,2678,True)
    r.over(0x1430,ITEM,4394,2678);r.over(0x1434,ITEM,4396,2678);r.over(0xC3C,ITEM,4395,2677);r.urn(4393,2683,0x1AFE);r.urn(4397,2683,0x1AFC)
    r.moss(4395,2682,hue=RUG);r.glow(4393,2673);r.sinkhole(4396,2683,0x30EA);r.crab(4393,2681,0x44D1)

    r=room('east','tack_room')                 # 4465-4470 x 2625-2635; west wall x=4463 (y<=2631)
    r.west(0xC39,ITEM,4464,2626);r.lamp(4464,2628,True,True);r.west(0x3CC1,SHELF,4464,2630);r.surf(0x1373,ITEM,4464,2630,-115)
    r.add(0xF37,ARMOR,4466,2626);r.add(0xF38,ARMOR,4469,2626);r.add(0x1378,ARMOR,4466,2629);r.add(0x1379,ARMOR,4469,2629);r.over(0x1374,ARMOR,4467,2627);r.over(0x166E,ITEM,4468,2628);r.over(0xFB6,ARMOR,4467,2631);r.over(0x14F8,0,4470,2631)
    r.hay(4468,2632);r.chest(4465,2634,0xE42);r.add(0xA2A,WOOD,4470,2634);r.mushrooms(4466,2634);r.moss(4469,2634,hue=RUG);r.crab(4465,2632,0x531F);r.cracks(4468,2631)

    r=room('south','market_stalls')             # 4430-4453 x 2649-2653; walls at x=4439 and x=4447 divide three stalls
    r.table(4431,2650,3,'log',contents=((0xF95,0),(0xF9A,1),(0x1760,2)));r.add(0x24D7,ITEM,4435,2650);r.surf(0xE1D,ITEM,4435,2650,-126);r.lamppost(4437,2652);r.over(0x175E,ITEM,4432,2652);r.add(0xA2A,WOOD,4433,2651)
    r.lantern(4440,2650,True);r.table(4441,2650,3,'log',contents=((0xB48,0,MARBLE),(0x1AFC,1),(0xB46,2,MARBLE)));r.add(0x1AFE,ITEM,4445,2650);r.add(0x241C,MARBLE,4445,2652);r.over(0xE44,ITEM,4441,2652);r.add(0xA2A,WOOD,4443,2651)
    r.west(0xC39,ITEM,4448,2652);r.table(4449,2650,3,'log',contents=((0x9D1,0),(0x9D0,1),(0x9D2,2)));r.add(0x24D9,ITEM,4453,2650);r.surf(0x9EB,ITEM,4453,2650,-126);r.barrel(4453,2652,0x154D);r.over(0x9D0,ITEM,4451,2652);r.add(0xA2A,WOOD,4451,2651)
    r.skeleton(4434,2653,0x123F);r.moss(4442,2653,hue=RUG);r.crab(4449,2653,0x44D1);r.cracks(4436,2649);r.mushrooms(4452,2653);r.over(0xEEB,0,4444,2653)

    r=room('south','ossuary_nook')              # 4433-4437 x 2665-2670; west wall x=4432
    r.tapestry(4432,2666,True,0xFDB);r.lamp(4432,2669,True);r.stone_bench(4436,2666);r.urn(4437,2670,0x241D);r.skulls(4434,2670);r.over(0x1853,ITEM,4436,2668);r.moss(4435,2668,hue=RUG);r.crab(4437,2665,0x531F);r.cracks(4434,2667)

def main():
    import sys
    import json
    manifest=WORLD/'Data/Atlantis/Decorations.json'
    if manifest.exists() and '--force' not in sys.argv:
        try:saved=json.loads(manifest.read_text(encoding='utf-8')).get('savedInGame')
        except Exception:saved=True
        if saved:raise SystemExit('Decorations.json has been edited from the game with [AtlantisSave. Re-run with --force to overwrite it from these layouts.')
    cells=load_statics();rooms=author_rooms()
    clear={p for p,ss in cells.items() if any(is_floor(s) for s in ss) and all(is_floor(s) for s in ss)}
    footprints=[];errors=[];blocked=set();unique=set()
    for r in rooms:
        if not r.items:errors.append((r.key,'empty'));continue
        xs=[p[2] for p in r.items];ys=[p[3] for p in r.items]
        if max(xs)-min(xs)>32 or max(ys)-min(ys)>32 or len(r.items)>300:errors.append((r.key,'too large',len(r.items),max(xs)-min(xs),max(ys)-min(ys)))
        area={(x,y) for x in range(min(xs)-1,max(xs)+2) for y in range(min(ys)-1,max(ys)+2)}
        for p in area:
            if any(not(is_floor(s) or is_wall(s)) for s in cells.get(p,[])):errors.append((r.key,'existing detail',p))
        for other,anchors in footprints:
            if anchors&area:errors.append((r.key,'overlaps room',other))
        footprints.append((r.key,{(p[2],p[3]) for p in r.items}))
        floors=Counter()
        for i,h,x,y,z,mode in r.items:
            x0,y0,x1,y1=SECTIONS[r.district]
            if not(x0<=x<x1 and y0<=y<y1):errors.append((r.key,'district',x,y))
            if (x,y) not in clear:errors.append((r.key,'floor',x,y))
            if mode in ('north','west'):
                back=(x,y-1) if mode=='north' else (x-1,y)
                if not any(is_wall(s) for s in cells.get(back,[])):errors.append((r.key,'wall backing',x,y,mode))
            if mode=='surface' and not any(xx==x and yy==y and zz+height(ii)==z and zz<z and mm in ('floor','north','west') for ii,hh,xx,yy,zz,mm in r.items):errors.append((r.key,'support',x,y,z))
            flags,ht=metadata(i)
            if mode=='overlay' and (ht>1 or flags&0x40):errors.append((r.key,'overlay art',hex(i),x,y))
            if mode=='floor':
                floors[(x,y)]+=1
                if floors[(x,y)]>1:errors.append((r.key,'two floor items',x,y,[hex(ii) for ii,hh,xx,yy,zz,mm in r.items if (xx,yy)==(x,y) and mm=='floor']))
            identity=(i,x,y,z)
            if identity in unique:errors.append((r.key,'duplicate',identity))
            unique.add(identity)
            if mode=='floor' or (mode in ('north','west') and flags&0x40 and z+ht>-127):blocked.add((x,y))
    if errors:
        for error in errors[:120]:print(error)
        raise SystemExit('%d layout errors'%len(errors))
    for component in connected_components(clear):
        remaining=component-blocked
        parts=connected_components(remaining)
        big=[p for p in parts if len(p)>3]
        if len(big)>1:
            for part in sorted(big,key=len)[:-1]:
                frontier=sorted({(x+dx,y+dy) for x,y in part for dx,dy in ((-1,0),(1,0),(0,-1),(0,1)) if (x+dx,y+dy) in blocked and (x+dx,y+dy) in component})
                owners={}
                for r in rooms:
                    for i,h,x,y,z,m in r.items:
                        if (x,y) in frontier and m=='floor':owners.setdefault((x,y),[]).append((r.key.split('_v2_')[1],hex(i)))
                print('SPLIT component of',len(part),'tiles; blocking furniture:',[(t,owners.get(t)) for t in frontier][:30])
            raise SystemExit('Furniture splits a walking route')
    rows=[]
    for r in rooms:
        for i,h,x,y,z,m in r.items:rows.append('  {"district":"%s","group":"%s","art":"0x%04X","hue":%d,"x":%d,"y":%d,"z":%d,"mode":"%s"}'%(r.district,r.key,i,h,x,y,z,m))
    text='{\n "map": "Atlantis (logical map 6, file index 1), lower floor Z -127",\n "format": 2,\n "savedInGame": false,\n "lastSave": "",\n "placements": [\n'+',\n'.join(rows)+'\n ]\n}\n'
    manifest.write_text(text,encoding='utf-8')
    print('Wrote',sum(len(r.items) for r in rooms),'items in',len(rooms),'rooms')
    print('Items by district:',{d:sum(len(r.items) for r in rooms if r.district==d) for d in SECTIONS})
    print('Rooms:',', '.join('%s(%d)'%(r.key.split('_v2_')[1],len(r.items)) for r in rooms))
    print('Validated floor, wall backing, raised support, overlay art, existing-detail protection and walking connectivity.')
if __name__=='__main__':main()
