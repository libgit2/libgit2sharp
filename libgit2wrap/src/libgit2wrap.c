#include "libgit2wrap.h"

int wrapped_git_repository_open(git_repository** repo_out, const char* path)
{
	git_repository *repo;
	int error = git_repository_open(&repo, path);

	*repo_out = repo;
	return error;
}

int wrapped_git_repository_open2(git_repository** repo_out, const char *git_dir, const char *git_object_directory, const char *git_index_file, const char *git_work_tree)
{
	git_repository *repo;
	int error = git_repository_open2(&repo, git_dir, git_object_directory, git_index_file, git_work_tree);

	*repo_out = repo;
	return error;
}
void wrapped_git_repository_free(git_repository* repo)
{
	git_repository_free(repo);
}

int wrapped_git_odb_exists(git_repository* repo, const char* raw_id)
{
	git_odb *odb;
	git_oid id;
	int error;

	odb = git_repository_database(repo);

	error = git_oid_mkstr(&id, raw_id);
	if (error != GIT_SUCCESS)
		return error;

	return git_odb_exists(odb, &id);
}

int wrapped_git_odb_read_header(git_rawobj* obj_out, git_repository* repo, const char *raw_id)
{
	git_odb *odb;
	git_oid id;
	int error;

	odb = git_repository_database(repo);

	error = git_oid_mkstr(&id, raw_id);
	if (error != GIT_SUCCESS)
		return error;

	error = git_odb_read_header(obj_out, odb, &id);
	if (error != GIT_SUCCESS)
		return error;

	return error;
}

int wrapped_git_odb_read(git_rawobj* obj_out, git_repository* repo, const char *raw_id)
{
	git_odb *odb;
	git_oid id;
	int error;

	odb = git_repository_database(repo);

	error = git_oid_mkstr(&id, raw_id);
	if (error != GIT_SUCCESS)
		return error;

	error = git_odb_read(obj_out, odb, &id);
	if (error != GIT_SUCCESS)
		return error;

	return error;
}