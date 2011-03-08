#include "libgit2wrap.h"
#include <assert.h>

int wrapped_git_repository_init(git_repository** repo_out, const char* path, unsigned int is_bare)
{
	git_repository *repo;
	int error = git_repository_init(&repo, path, is_bare);
	if (error < GIT_SUCCESS)
		return error;

	*repo_out = repo;
	return error;
}

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

static void force_commit_parse(git_commit* commit)
{
	// Warning! Hacky... This forces the full parse of the commit :-/
	const char *message;
	message = git_commit_message(commit);
}

static void cascade_force_full_commit_parse(git_object *object)
{
	switch(git_object_type(object))
	{
	case GIT_OBJ_TAG:
		cascade_force_full_commit_parse((git_object*)git_tag_target((git_tag*)object));
		break;

	case GIT_OBJ_COMMIT:
		force_commit_parse((git_commit *)object);
		break;

	case GIT_OBJ_BLOB:
		/* Fallthrough */
	case GIT_OBJ_TREE:
		/* Fallthrough */
	default:
		break;
	}

	return;
}

int wrapped_git_repository_lookup__internal(git_object** obj_out, git_repository* repo, const git_oid* id, git_otype type)
{
	int error = GIT_SUCCESS;

	error = git_object_lookup(obj_out, repo, id, type);
	if (error != GIT_SUCCESS)
		return error;

	cascade_force_full_commit_parse(*obj_out);

	return error;
}

int wrapped_git_repository_lookup(git_object** obj_out, git_otype *type_out, git_repository* repo, const char* raw_id)
{
	git_oid id;
	int error = GIT_SUCCESS;

	error = git_oid_mkstr(&id, raw_id);
	if (error != GIT_SUCCESS)
		return error;

	error = wrapped_git_repository_lookup__internal(obj_out, repo, &id, GIT_OBJ_ANY);
	if (error < GIT_SUCCESS)
		return error;

	*type_out = git_object_type(*obj_out);
	return error;
}

int wrapped_git_reference_lookup(git_reference** ref_out, git_rtype *type_out,  git_repository* repo, const char* reference_name, int should_recursively_peel)
{
	git_reference *reference, *resolved_ref;
	int error = GIT_SUCCESS;

	error = git_reference_lookup(&reference, repo, reference_name);
	if (error < GIT_SUCCESS)
		return error;
	
	resolved_ref = reference;

	if (should_recursively_peel) {
		error = git_reference_resolve(&resolved_ref, reference);
		if (error < GIT_SUCCESS)
			return error;
	}

	*type_out = git_reference_type(resolved_ref);
	*ref_out = resolved_ref;
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

int wrapped_git_apply_tag(git_tag** tag_out, git_repository* repo, const char *raw_target_id, const char *tag_name, const char *tag_message, const char *tagger_name, const char *tagger_email, time_t tagger_time, int tagger_timezone_offset)
{
	git_oid id;
	int error = GIT_SUCCESS;
	git_object* target;
	git_tag* tag;

	const git_signature* tagger;

	error = git_oid_mkstr(&id, raw_target_id);
	if (error != GIT_SUCCESS)
		return error;

	error = wrapped_git_repository_lookup__internal(&target, repo, &id, GIT_OBJ_ANY);
	if (error != GIT_SUCCESS)
		return error;

	error = git_tag_new(&tag, repo);
	if (error != GIT_SUCCESS)
		return error;

	tagger = git_signature_new(tagger_name, tagger_email, tagger_time, tagger_timezone_offset);

	git_tag_set_tagger(tag, tagger);
	git_tag_set_name(tag, tag_name);
	git_tag_set_target(tag, target);
	git_tag_set_message(tag, tag_message);

	error = git_object_write((git_object*)tag);
	if (error != GIT_SUCCESS)
		return error;

	*tag_out = tag;

	return error;
}