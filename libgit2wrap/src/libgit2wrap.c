#include "libgit2wrap.h"
#include <assert.h>

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

int wrapped_git_repository_lookup(git_object** obj_out, git_repository* repo, const char* raw_id, git_otype type)
{
	git_oid id;
	git_odb *odb;
	int error = GIT_SUCCESS;
	git_tag* tag;
	git_object* target;

	odb = git_repository_database(repo);

	error = git_oid_mkstr(&id, raw_id);
	if (error != GIT_SUCCESS)
		return error;

	error = git_repository_lookup(obj_out, repo, &id, type);
	if (error != GIT_SUCCESS)
		return error;

	if (git_object_type(*obj_out) == GIT_OBJ_COMMIT)
	{
		// Warning! Hacky... This forces the full parse of the commit :-/
		const char *message;
		message = git_commit_message((git_commit*)(*obj_out));
	} 
	else if (git_object_type(*obj_out) == GIT_OBJ_TAG)
	{
		tag = (git_tag*)(*obj_out);
		target = (git_object*)git_tag_target(tag);

		if (git_object_type(target) == GIT_OBJ_COMMIT)
		{
			// Warning! Hacky... This forces the full parse of the commit :-/
			const char *message;
			message = git_commit_message((git_commit*)(target));
		}
	}

	return error;
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

int wrapped_git_odb_read(git_rawobj* obj_out, git_repository* repo, const char* raw_id)
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

int wrapped_git_apply_tag(git_tag** tag_out, git_repository* repo, const char *raw_target_id, const char *tag_name, const char *tag_message, const char *tagger_name, const char *tagger_email, time_t tagger_time)
{
	git_oid id;
	int error;
	git_object* target;
	git_tag* tag;

	error = git_oid_mkstr(&id, raw_target_id);
	if (error != GIT_SUCCESS)
		return error;

	error = git_repository_lookup(&target, repo, &id, GIT_OBJ_ANY);
	if (error != GIT_SUCCESS)
		return error;

	if (git_object_type(target) == GIT_OBJ_COMMIT)
	{
		// Warning! Hacky... This forces the full parse of the commit :-/
		const char *message;
		message = git_commit_message((git_commit*)(target));
	}

	error = git_tag_new(&tag, repo);
	if (error != GIT_SUCCESS)
		return error;

	git_tag_set_tagger(tag, tagger_name, tagger_email, tagger_time);
	git_tag_set_name(tag, tag_name);
	git_tag_set_target(tag, target);
	git_tag_set_message(tag, tag_message);

	error = git_object_write((git_object*)tag);
	if (error != GIT_SUCCESS)
		return error;

	*tag_out = tag;

	return error;
}