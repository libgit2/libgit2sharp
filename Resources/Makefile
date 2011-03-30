
all:
	gccxml -I../libgit2/include/ libgit2.h -fxml=libgit2.xml

patch:
	cp formatting.patch ../libgit2/formatting.patch
	patch -p1 -i formatting.patch --directory=../libgit2/
	rm ../libgit2/formatting.patch

clean:
	rm libgit2.xml
